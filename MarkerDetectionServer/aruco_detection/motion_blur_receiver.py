import socket
import struct
import threading
from queue import Queue

class HMDMotionReceiver:
    """Receives HMD motion data from Unity"""
    def __init__(self, host='127.0.0.1', port=12345):
        self.socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.socket.bind((host, port))
        self.latest_motion = None
        self.running = True
        self.motion_queue = Queue(maxsize=1)  # Only keep latest motion data
        
        # Start receiver thread
        self.thread = threading.Thread(target=self._receive_loop, daemon=True)
        self.thread.start()
    
    def _receive_loop(self):
        while self.running:
            try:
                data = self.socket.recv(1024)
                # Assuming Unity sends: timestamp, pos(x,y,z), vel(x,y,z), angular_vel(x,y,z)
                motion_data = struct.unpack('10f', data)
                if self.motion_queue.full():
                    self.motion_queue.get()  # Remove old data
                self.motion_queue.put(motion_data)
            except:
                pass
    
    def get_latest_motion(self):
        try:
            return self.motion_queue.get_nowait()
        except:
            return None
    
    def close(self):
        self.running = False
        self.socket.close()