from collections import deque
import cv2
import cv2.aruco as aruco
import numpy as np


class ArucoDetecter:
    def __init__(self, mtx, dist, marker_size, selected_dictionary):
        self._mtx = mtx
        self._dist = dist
        self.marker_size = marker_size
        self._detecter = aruco.ArucoDetector(
            aruco.getPredefinedDictionary(selected_dictionary))

    def detect(self, frame):

        def calculate_center_and_axis(marker_size, r_vec, t_vec):
            center = [0, 0, 0]
            x_edge = [marker_size/2, 0, 0]
            y_edge = [0, marker_size/2, 0]
            z_edge = [0, 0, marker_size/2]
            obj_points = [center, x_edge, y_edge, z_edge]

            return _calculate_world_coordinate(obj_points, r_vec, t_vec)
        
        detected_results = []
        (corners, ids, rejected) = self._detecter.detectMarkers(frame)

        if len(corners) <= 0:
            return []
        
        for i, marker_id in enumerate(ids):
            r_vec, t_vec, obj_points = aruco.estimatePoseSingleMarkers(
                corners[i], 
                self.marker_size, 
                self._mtx, 
                self._dist)
            predicts = calculate_center_and_axis(
                self.marker_size,
                r_vec,
                t_vec)
            predicts = deque(predicts)
            predicts.appendleft(int(marker_id))
            predicts = list(predicts)
            detected_results.append([predicts, r_vec, t_vec])

        return detected_results


def _calculate_world_coordinate(obj_points, r_vec, t_vec):
    predicts = []
    r_mat, jacobian = cv2.Rodrigues(r_vec)

    for obj_point in obj_points:
        obj_points_corner = np.dot(r_mat, np.asarray(obj_point)) + t_vec
        predicts.extend(obj_points_corner.flatten())

    return predicts
