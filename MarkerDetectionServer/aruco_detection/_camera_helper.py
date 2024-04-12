import cv2
from typing import Dict


def generate_camera_stream(settings: Dict):
    camera_resolution = tuple(settings['camera']['resolution'])
    cap = cv2.VideoCapture(settings['camera']['port'], cv2.CAP_DSHOW) 
    cap.set(cv2.CAP_PROP_FPS, settings['camera']['fps'])
    cap.set(cv2.CAP_PROP_FRAME_HEIGHT, camera_resolution[0])
    cap.set(cv2.CAP_PROP_FRAME_WIDTH, camera_resolution[1])
    flip_code = settings['camera']['flip_code']
    return cap, camera_resolution, flip_code

def nullable_flip(frame, flip_code):

    if flip_code > 1:
        return frame

    return cv2.flip(frame, flip_code)
