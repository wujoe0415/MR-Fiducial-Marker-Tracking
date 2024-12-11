import numpy as np
from filterpy.kalman import KalmanFilter
from scipy.spatial.distance import cdist
from _aruco_detecter import ArucoDetecter

class KalmanArucoTracker:
    def __init__(self, process_noise=0.1, measurement_noise=0.1):
        """
        Initialize Kalman filter for tracking ArUco markers in 3D space.
        State vector: [x, y, z, vx, vy, vz]
        """
        self.trackers = {}  # Dictionary to store KalmanFilter objects for each marker
        self.process_noise = process_noise
        self.measurement_noise = measurement_noise
        self.max_tracking_distance = 0.5  # Maximum distance for associating detections
        
    def _create_kalman_filter(self):
        """Create a new Kalman filter for 3D position tracking"""
        kf = KalmanFilter(dim_x=6, dim_z=3)  # State: [x,y,z,vx,vy,vz], Measurement: [x,y,z]
        
        # State transition matrix
        kf.F = np.array([
            [1, 0, 0, 1, 0, 0],
            [0, 1, 0, 0, 1, 0],
            [0, 0, 1, 0, 0, 1],
            [0, 0, 0, 1, 0, 0],
            [0, 0, 0, 0, 1, 0],
            [0, 0, 0, 0, 0, 1]
        ])
        
        # Measurement matrix
        kf.H = np.array([
            [1, 0, 0, 0, 0, 0],
            [0, 1, 0, 0, 0, 0],
            [0, 0, 1, 0, 0, 0]
        ])
        
        # Measurement noise
        kf.R *= self.measurement_noise
        
        # Process noise
        kf.Q *= self.process_noise
        
        # Initial state covariance
        kf.P *= 1.0
        
        return kf
    
    def update(self, detections):
        """
        Update trackers with new detections
        Args:
            detections: List of [marker_id, center_x, center_y, center_z, ...]
        Returns:
            Dictionary of filtered positions for each marker
        """
        if not detections:
            # Predict next state for all existing trackers
            for marker_id in self.trackers:
                self.trackers[marker_id].predict()
            return {}
            
        # Extract marker positions
        current_markers = {}
        for detection in detections:
            marker_id = detection[0]
            position = np.array(detection[1:4]).reshape(3, 1)  # Only use center position [x,y,z]
            current_markers[marker_id] = position
            
        # Update existing trackers and create new ones
        filtered_positions = {}
        for marker_id, position in current_markers.items():
            if marker_id not in self.trackers:
                # Create new tracker
                kf = self._create_kalman_filter()
                kf.x[:3] = position  # Initialize position
                self.trackers[marker_id] = kf
            
            # Update tracker
            kf = self.trackers[marker_id]
            kf.predict()
            kf.update(position)
            
            # Store filtered position
            filtered_positions[marker_id] = {
                'position': kf.x[:3].reshape(3,),
                'velocity': kf.x[3:].reshape(3,),
                'covariance': kf.P[:3,:3]
            }
            
        return filtered_positions

class EnhancedArucoDetecter(ArucoDetecter):
    def __init__(self, mtx, dist, marker_size, selected_dictionary):
        super().__init__(mtx, dist, marker_size, selected_dictionary)
        self.tracker = KalmanArucoTracker()
        
    def detect(self, frame):
        # Get raw detections from parent class
        detected_results = super().detect(frame)
        
        if not detected_results:
            return []
            
        # Format detections for Kalman filter
        formatted_detections = []
        for result in detected_results:
            predicts, r_vec, t_vec = result
            formatted_detections.append(predicts)  # predicts already contains [id, center_x, center_y, center_z, ...]
            
        # Update Kalman filters
        filtered_positions = self.tracker.update(formatted_detections)
        
        # Update detected results with filtered positions
        filtered_results = []
        for result in detected_results:
            predicts, r_vec, t_vec = result
            marker_id = predicts[0]
            
            if marker_id in filtered_positions:
                filtered_pos = filtered_positions[marker_id]['position']
                # Update center position (first 3 coordinates after marker_id)
                predicts[1:4] = filtered_pos
                
            filtered_results.append([predicts, r_vec, t_vec])
            
        return filtered_results