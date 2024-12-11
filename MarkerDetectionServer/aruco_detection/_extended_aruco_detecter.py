import numpy as np
from scipy.linalg import block_diag
from filterpy.kalman import ExtendedKalmanFilter
from _aruco_detecter import ArucoDetecter

class ExtendedKalmanArucoTracker:
    def __init__(self, process_noise=0.1, measurement_noise=0.1, dt=1/30.0):
        """
        Initialize Extended Kalman filter for tracking ArUco markers with non-linear motion.
        State vector: [x, y, z, vx, vy, vz, ax, ay, az]
        """
        self.trackers = {}
        self.process_noise = process_noise
        self.measurement_noise = measurement_noise
        self.dt = dt

    def _create_ekf(self):
        """Create a new Extended Kalman filter"""
        ekf = ExtendedKalmanFilter(dim_x=9, dim_z=3)
        
        # Initialize state covariance matrix
        ekf.P = np.diag([1.0, 1.0, 1.0,       # position variance
                         0.1, 0.1, 0.1,        # velocity variance
                         0.1, 0.1, 0.1])       # acceleration variance
        
        # Measurement noise
        ekf.R = np.eye(3) * self.measurement_noise
        
        # Process noise - corrected to match state dimensions
        q = self.process_noise * np.array([
            [self.dt**4/4, self.dt**3/2, self.dt**2],
            [self.dt**3/2, self.dt**2,   self.dt],
            [self.dt**2,   self.dt,      1.0]
        ])
        ekf.Q = np.kron(np.eye(3), q)  # Create 9x9 block diagonal matrix
        
        # State transition matrix
        ekf.F = np.eye(9)
        ekf.F[0:3, 3:6] = np.eye(3) * self.dt
        ekf.F[3:6, 6:9] = np.eye(3) * self.dt
        
        return ekf

    def h(self, x):
        """Measurement function"""
        return x[0:3]

    def HJacobian(self, x):
        """Measurement Jacobian"""
        H = np.zeros((3, 9))
        H[0:3, 0:3] = np.eye(3)
        return H

    def update(self, detections):
        """Update trackers with new detections"""
        if not detections:
            # Predict only for existing trackers when no detections
            predicted_positions = {}
            for marker_id in self.trackers:
                ekf = self.trackers[marker_id]
                ekf.predict()
                predicted_positions[marker_id] = {
                    'position': ekf.x[0:3],
                    'velocity': ekf.x[3:6],
                    'acceleration': ekf.x[6:9],
                    'covariance': ekf.P[0:3, 0:3]
                }
            return predicted_positions

        filtered_positions = {}
        for detection in detections:
            marker_id = detection[0]
            position = np.array(detection[1:4]).reshape(3,1)  # Changed to 1D array

            if marker_id not in self.trackers:
                # Initialize new tracker
                ekf = self._create_ekf()
                ekf.x[0:3] = position
                self.trackers[marker_id] = ekf
            
            ekf = self.trackers[marker_id]
            
            # Predict and update steps
            ekf.predict()
            ekf.update(position, HJacobian=self.HJacobian, Hx=self.h)

            # Store results
            filtered_positions[marker_id] = {
                'position': ekf.x[0:3],
                'velocity': ekf.x[3:6],
                'acceleration': ekf.x[6:9],
                'covariance': ekf.P[0:3, 0:3]
            }

        return filtered_positions

class EnhancedArucoDetecterNonLinear(ArucoDetecter):
    def __init__(self, mtx, dist, marker_size, selected_dictionary):
        super().__init__(mtx, dist, marker_size, selected_dictionary)
        self.tracker = ExtendedKalmanArucoTracker()
        
    def detect(self, frame):
        detected_results = super().detect(frame)
        if not detected_results:
            return []
            
        formatted_detections = []
        for result in detected_results:
            predicts, r_vec, t_vec = result
            formatted_detections.append(predicts)
            
        filtered_positions = self.tracker.update(formatted_detections)
        
        filtered_results = []
        for result in detected_results:
            predicts, r_vec, t_vec = result
            marker_id = predicts[0]
            
            if marker_id in filtered_positions:
                filtered_pos = filtered_positions[marker_id]['position']
                predicts[1:4] = filtered_pos.flatten()  # Ensure 1D array
                
            filtered_results.append([predicts, r_vec, t_vec])
            
        return filtered_results