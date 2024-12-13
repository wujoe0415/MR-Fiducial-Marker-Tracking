import argparse
from ctypes import c_bool
import cv2
import json
from multiprocessing import Process, Pipe, Value, shared_memory
import numpy as np
import os
import socket
from _aruco_detecter import ArucoDetecter
from _enhanced_detecter import EnhancedArucoDetecter
from _extended_aruco_detecter import EnhancedArucoDetecterNonLinear
from _motion_aware_aruco_detecter import HMDMotionAwareArucoDetecter
from _camera_helper import *
import struct

POLL_TIME = 0.00001


def poll_recv(pipe_coonn1):

    if pipe_coonn1.poll(POLL_TIME):
        return (True, pipe_coonn1.recv())

    return (False, None)


def aruco_detect(
        mtx,
        dist,
        marker_size,
        selected_dictionary, 
        frame_conn, 
        result_conn, 
        end_signal):
    continue_flag = True
    # change aruco detecter
    aruco_detecter = HMDMotionAwareArucoDetecter(
        mtx,
        dist,
        marker_size,
        selected_dictionary)

    while not end_signal.value:
        ret, frame = poll_recv(frame_conn)

        if not ret:
            continue
        
        detected_results = aruco_detecter.detect(frame)
        result_conn.send(detected_results)


def multi_size_aruco_detect(settings, result_conn, end_signal):
    mtx = np.array(settings['camera']['mtx'])
    dist = np.array(settings['camera']['dist'])
    marker_dictionary_list = list(settings['marker'])
    setting_counts = len(marker_dictionary_list)
    aruco_detecter_processes = []
    frame_conns = []
    result_conns = []
    sub_process_end_signal = Value(c_bool, False)

    def detect_result_visualize(frame, center_pos, marker_size):
        center_object_point = np.array([0, 0, 0], dtype=np.double)
        img_points, jacobian = cv2.projectPoints(
            center_object_point,
            r_vec,
            t_vec,
            mtx,
            dist)
        center_coord = (img_points[0]).flatten().astype(int)
        distance_to_center = np.linalg.norm(center_pos)
        cv2.putText(
            frame,
            ' D: ~%scm'%(int(distance_to_center * 100)),
            center_coord,
            cv2.FONT_HERSHEY_SIMPLEX,
            marker_size*15,
            (255, 255, 255),
            1,
            cv2.LINE_AA)
        cv2.drawFrameAxes(frame, mtx, dist, r_vec, t_vec, marker_size)
        return frame

    for i in range(setting_counts):
        frame_conn1, frame_conn2 = Pipe(duplex=False)
        result_conn1, result_conn2 = Pipe(duplex=False)
        frame_conns.append(frame_conn2)
        result_conns.append(result_conn1)
        selected_dictionary = getattr(cv2.aruco, list(settings['marker'])[i])
        marker_size = settings['marker'][marker_dictionary_list[i]]
        aruco_detect_process = Process(
            target=aruco_detect,
            args=(
                mtx, 
                dist,
                marker_size,
                selected_dictionary,
                frame_conn1, 
                result_conn2, 
                sub_process_end_signal),
            daemon=True)
        aruco_detect_process.start()
        aruco_detecter_processes.append(aruco_detect_process)

    cap, camera_resolution, flip_code = generate_camera_stream(settings)

    while True:

        if not (cap.isOpened()):
            end_signal.value = True

        if end_signal.value:
            sub_process_end_signal.value = True
            for i in range(setting_counts):
                aruco_detecter_processes[i].join()
            break
        
        ret, data = poll_recv(result_conn)
        if ret and isinstance(data, tuple) and data[0] == 'hmd_data':
            hmd_motion = data[1]
            for i in range(setting_counts):
                frame_conns[i].send(('hmd_motion', hmd_motion))
            continue

        ret, frame = cap.read()
        frame = nullable_flip(frame, flip_code)
        results = []
        marker_counts = 0

        for i in range(setting_counts):
            frame_conns[i].send(frame)

        for i in range(setting_counts):
            selected_dictionary = getattr(
                cv2.aruco,
                list(settings['marker'])[i])
            marker_size = settings['marker'][marker_dictionary_list[i]]
            detected_results = result_conns[i].recv()
            marker_counts += len(detected_results)

            for detected_result in detected_results:
                prediction_content, r_vec, t_vec = detected_result
                results.extend([i] + prediction_content)
                frame = detect_result_visualize(frame, prediction_content[1:4], marker_size)

        cv2.imshow('Frame', frame)
        keycode = cv2.waitKey(1)

        if keycode == ord('q'):
            end_signal.value = True

        if end_signal.value:
            continue

        message = np.array([marker_counts] + results, dtype=np.double)
        result_conn.send(message)

    end_signal.value = True


def empty_server(port, result_conn, end_signal):

    while not end_signal.value:
        _ = poll_recv(result_conn)


def data_sending_server(port, result_conn, end_signal):
    SOCKET_TYPE = socket.SOCK_STREAM
    socket_connection = None
    connected = False
    
    def try_connect(
            socket_family,
            ip_address,
            socket_type=SOCKET_TYPE,
            port=port):
        socket_connection = None

        try:
            socket_connection = socket.socket(socket_family, socket_type)
        except OSError:
            return (False, None)

        try:
            socket_connection.connect((ip_address, port))
            socket_connection.setblocking(False)
        except OSError:
            return (False, None)

        return (True, socket_connection)
    
    connected, socket_connection = try_connect(
        socket_family=socket.AF_INET6, 
        ip_address='::1')
        
    if not connected:
        connected, socket_connection = try_connect(
            socket_family=socket.AF_INET, 
            ip_address='127.0.0.1')

    if not connected:
        print('Failed to make data transmission connection.')
        end_signal.value = True
        return
    
    recv_buffer = 'b'
    HEADER_SIZE = 8

    while not end_signal.value:
        ret, message = poll_recv(result_conn)

        # send data
        if ret:
            try:
                socket_connection.sendall(message.tobytes())
            except (ConnectionResetError, ConnectionAbortedError):
                print('Connection reset.')
                end_signal.value = True
                poll_recv(result_conn) # TODO: Check if this is necessary
                break
        
        try:
            chunk = socket_connection.recv(4096)
            if chunk:
                recv_buffer += chunk
                
                # Process complete messages
                while len(recv_buffer) >= HEADER_SIZE:
                    # Extract message length
                    msg_len = struct.unpack('!Q', recv_buffer[:HEADER_SIZE])[0]
                    
                    # Check if we have a complete message
                    if len(recv_buffer) >= HEADER_SIZE + msg_len:
                        # Extract message
                        msg_data = recv_buffer[HEADER_SIZE:HEADER_SIZE + msg_len]
                        recv_buffer = recv_buffer[HEADER_SIZE + msg_len:]
                        
                        # Parse HMD data
                        hmd_data = np.frombuffer(msg_data, dtype=np.float64)
                        # Format: [timestamp, pos_x, pos_y, pos_z, vel_x, vel_y, vel_z, ang_vel_x, ang_vel_y, ang_vel_z]
                        
                        # Store HMD data in shared memory or send through pipe
                        result_conn.send(('hmd_data', hmd_data))
                    else:
                        break
        except BlockingIOError:
            # No data available to receive
            pass
        except (ConnectionResetError, ConnectionAbortedError):
            print('Connection lost.')
            end_signal.value = True
            break
    
    end_signal.value = True
    socket_connection.shutdown(socket.SHUT_WR)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('-mode', '-m', default='auto', type=str)
    parser.add_argument(
        '--setting_file_path',
        '-s',
        default='setting.json',
        type=str)
    args = parser.parse_args()
    mode = args.mode
    setting_file_path = args.setting_file_path
    server_target = None

    if mode == 'debug':
        server_target = empty_server
    else:
        server_target = data_sending_server

    if not os.path.exists(setting_file_path):
        print('No setting file')
        return

    settings = None

    with open(setting_file_path) as setting_file:
        settings = json.load(setting_file)

    end_signal = Value(c_bool, False)
    result_conn1, result_conn2 = Pipe(duplex=False)
    aruco_detect_process = Process(
        target=multi_size_aruco_detect,
        args=(settings, result_conn2, end_signal),
        daemon=False)
    data_transmission_process = Process(
        target=server_target,
        args=(settings['connection']['port'], result_conn1, end_signal),
        daemon=True)
    aruco_detect_process.start()
    data_transmission_process.start()
    aruco_detect_process.join()
    data_transmission_process.join()
    print('Detecter close.')

if __name__ == '__main__':
    main()
