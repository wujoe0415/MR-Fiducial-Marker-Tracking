import argparse
from ctypes import c_bool, c_int
import cv2
from datetime import datetime
from enum import IntEnum, unique
import json
from multiprocessing import Process, Value, shared_memory
import numpy as np
import os
import tkinter as tk
from _camera_calibrator import CameraCalibrator
from _camera_helper import *
from _multiprocessing_ui import ButtonUI


@unique
class _ExecuteSignal(IntEnum):
    NONE = 0
    TAKE_A_PHOTO = 1
    CALIBRATE = 2


def _ui_manage(execute_signal):
    root = tk.Tk()
    root.geometry('%dx%d+%d+%d'%(240, 80, 0, 0))
    
    def _on_close_frame():
        print('UI closed.')
        root.destroy()

    root.protocol('WM_DELETE_WINDOW', _on_close_frame) 
    ui = ButtonUI('Calibrator', root, execute_signal)
    ui.construct_button('Take a photo', _ExecuteSignal.TAKE_A_PHOTO)
    ui.construct_button('Calibrate camera', _ExecuteSignal.CALIBRATE)
    ui.mainloop()


def _calibrate_manage(setting_file_path, settings, execute_signal, end_signal):
    photos_path = settings['path']['photos_path']
    cap, camera_resolution, flip_code = generate_camera_stream(settings)
    camera_calibrator = None

    while(cap.isOpened()): 
        ret, frame = cap.read()

        if end_signal.value or (not ret):
            break

        frame = nullable_flip(frame, flip_code)
        cv2.imshow('Frame', frame) 
        keycode = cv2.waitKey(1)
        signal_value = _ExecuteSignal.NONE

        if execute_signal.value != _ExecuteSignal.NONE.value:
            signal_value = _ExecuteSignal(execute_signal.value).value
            execute_signal.value = _ExecuteSignal.NONE.value

        if signal_value == _ExecuteSignal.TAKE_A_PHOTO:
            current_time = datetime.now().strftime("%Y-%m-%d_%H-%M-%S")
            path_to_saving_file = ('%s/%s.jpg'%(photos_path, current_time))
            cv2.imwrite(path_to_saving_file, frame)
            print(('"%s.jpg" had been saved.'%(current_time)))

        if signal_value == _ExecuteSignal.CALIBRATE:
            file_list = os.listdir(photos_path)
            file_counts = len(file_list)

            if file_counts <= 0:
                print('No file in photos path.')
                continue

            print('Initialize camera calibrator...')
            
            if camera_calibrator == None:
                pattern_size = tuple(
                    settings['calibration_checkerboard']['pattern_size'])
                edge_size = settings['calibration_checkerboard']['edge_size']
                camera_calibrator = CameraCalibrator(pattern_size, edge_size)
            else:
                camera_calibrator.reset()

            print('Reading photos...')

            for i in range(file_counts):
                file_name = file_list[i]
                try:    
                    path_to_saved_file = ('%s/%s'%(photos_path, file_name))
                    img = cv2.imread(path_to_saved_file)
                    gray_img = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY) 
                except:
                    print(('Error while reading photo "%s".'%(file_name)))
                    continue
                
                ret = camera_calibrator.find_corners(gray_img)
                
                if not ret:
                    print('No corner found in "%s".'%(file_name))

            valid_photo_counts = camera_calibrator.get_pic_counts_string()
            print(('Valid photo counts: %s.'%(valid_photo_counts)))

            if valid_photo_counts == 0: 
                print('No valid photo.')
                continue

            calibration_finished = camera_calibrator.calibrate_camera(
                camera_resolution)
            
            if not calibration_finished:
                print('Calibration failed.')
                continue
            
            mtx, dist = camera_calibrator.get_param()

            with open(setting_file_path, 'r+') as setting_file:
                settings = json.load(setting_file)
                settings['camera'].update({'mtx': mtx.tolist()})
                settings['camera'].update({'dist': dist.tolist()})
                setting_file.seek(0)
                json.dump(settings, setting_file, indent=4)

            print('Calibration result saved to "%s".'%(setting_file_path))
                
    cap.release()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument(
        '--setting_file_path',
        '-s',
        default='setting.json',
        type=str)
    args = parser.parse_args()
    setting_file_path = args.setting_file_path

    if not os.path.exists(setting_file_path):
        print('No setting file in setting file path.')
        return

    settings = None

    with open(setting_file_path) as setting_file:
        settings = json.load(setting_file)

    photos_path = settings['path']['photos_path']

    if not os.path.exists(photos_path):
        os.makedirs(photos_path)

    execute_signal = Value(c_int, 0)
    end_signal = Value(c_bool, False)
    frame_dtype = np.uint8
    button_ui_process = Process(
        target=_ui_manage,
        args=(execute_signal,),
        daemon=True)
    button_ui_process.start()
    camera_process = Process(
        target=_calibrate_manage,
        args=(setting_file_path, settings, execute_signal, end_signal),
        daemon=True)
    camera_process.start()
    button_ui_process.join()
    end_signal.value = True
    camera_process.join()


if __name__ == '__main__':
    main()
