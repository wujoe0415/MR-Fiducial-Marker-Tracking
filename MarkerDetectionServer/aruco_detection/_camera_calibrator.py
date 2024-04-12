import cv2
import numpy as np 


class CameraCalibrator:
    SUPIX_TERM_CRITERIA = (
        cv2.TERM_CRITERIA_EPS + cv2.TERM_CRITERIA_MAX_ITER, 
        30,
        0.001)
    DEFAULT_MTX = np.array(
        [[600.0, 0.0, 300.0],[0.0, 600.0, 200.0],[0.0, 0.0, 1.0]])
    DEFAULT_DIST = np.array([[0.0, 0.0, 0.0, 0.0, 0.0]])
    
    def __init__(self, pattern_size, edge_size):
        self._world_points = []
        self._img_points = []
        self._mtx = CameraCalibrator.DEFAULT_MTX
        self._dist = CameraCalibrator.DEFAULT_DIST
        self._pattern_size = pattern_size
        self._checkerboard_size = self._pattern_size[0] * self._pattern_size[1]
        self._checkerboard_points = np.zeros(
            (1, self._checkerboard_size, 3), 
            np.float32)
        checkerboard_grid = np.mgrid[
            0:self._pattern_size[0], 
            0:self._pattern_size[1]]
        self._checkerboard_points[0,:,:2] = checkerboard_grid.T.reshape(-1, 2)
        self._checkerboard_points = self._checkerboard_points * edge_size
    
    def find_corners(self, frame):
        ret, corners = cv2.findChessboardCorners(frame, self._pattern_size)

        if not ret:
            return False
        
        subpix_corners = cv2.cornerSubPix(
            frame, 
            corners, 
            (11, 11), 
            (-1, -1), 
            CameraCalibrator.SUPIX_TERM_CRITERIA)
        
        self._world_points.append(self._checkerboard_points)
        self._img_points.append(corners)

        return True

    def calibrate_camera(self, resolution):

        if len(self._world_points) <= 0:
            return False

        ret, mtx, dist, rvecs, tvecs = cv2.calibrateCamera(
            self._world_points, 
            self._img_points, 
            resolution, 
            None, 
            None)

        if not ret:
            return ret

        self._mtx = mtx
        self._dist = dist

        return ret

    def get_param(self):
        return (self._mtx, self._dist)

    def get_pic_counts_string(self):
        return (str(len(self._world_points)))

    def reset(self):
        self._world_points = []
        self._img_points = []
        self._mtx = CameraCalibrator.DEFAULT_MTX
        self._dist = CameraCalibrator.DEFAULT_DIST
