import numpy as np
from scipy import fft
import cv2
from scipy.signal import wiener
from _extended_aruco_detecter import EnhancedArucoDetecterNonLinear
from queue import Queue
    

class HMDMotionAwareArucoDetecter(EnhancedArucoDetecterNonLinear):
    def __init__(self, mtx, dist, marker_size, selected_dictionary):
        super().__init__(mtx, dist, marker_size, selected_dictionary)
        self.camera_matrix = mtx
        self.image_size = (720, 1280) # Adjust to setting.json
        self.exposure_time = 1/500.0  # Adjust to camera settings
        self.latest_hmd_motion = None

    def update_hmd_motion(self, hmd_data):
        """Update latest HMD motion data"""
        self.latest_hmd_motion = hmd_data
    
    def _project_motion_to_image(self, pos, vel, angular_vel):
        """Project 3D HMD motion onto image plane"""
        # Convert world space velocity to camera space
        focal_length = self.camera_matrix[0,0]  # Assuming fx = fy
        
        # Linear motion contribution
        pixel_vel_x = focal_length * (vel[0] / pos[2])
        pixel_vel_y = focal_length * (vel[1] / pos[2])
        
        # Angular motion contribution (simplified - assumes small angles)
        # Only consider rotation around x and y axes as they cause most blur
        pixel_vel_x += angular_vel[1] * focal_length
        pixel_vel_y -= angular_vel[0] * focal_length
        
        return np.array([pixel_vel_x, pixel_vel_y])

    def _create_motion_psf(self, pixel_velocity):
        """Create PSF based on projected motion"""
        vel_mag = np.linalg.norm(pixel_velocity)
        if vel_mag < 0.1:  # Minimal motion threshold
            return np.ones((1, 1))
            
        # Calculate blur length based on velocity and exposure time
        blur_length = int(vel_mag * self.exposure_time)
        blur_length = min(blur_length, min(self.image_size) // 4)
        
        angle = np.arctan2(pixel_velocity[1], pixel_velocity[0])
        
        # Create PSF
        kernel_size = 2 * blur_length + 1
        psf = np.zeros((kernel_size, kernel_size))
        center = (blur_length, blur_length)
        
        for i in range(blur_length + 1):
            x = int(center[0] + np.cos(angle) * i)
            y = int(center[1] + np.sin(angle) * i)
            psf[y, x] = 1
            
            x = int(center[0] - np.cos(angle) * i)
            y = int(center[1] - np.sin(angle) * i)
            psf[y, x] = 1
            
        return psf / psf.sum()


    def detect(self, frame):
        if self.latest_hmd_motion is not None:
            # Extract motion data
            timestamp = self.latest_hmd_motion[0]
            pos = self.latest_hmd_motion[1:4]
            vel = self.latest_hmd_motion[4:7]
            angular_vel = self.latest_hmd_motion[7:10]
            
            # Project motion to image plane
            pixel_velocity = self._project_motion_to_image(pos, vel, angular_vel)
            
            # Create PSF and deblur
            psf = self._create_motion_psf(pixel_velocity)
            frame = self._deblur_frame(frame, psf)
            
        return super().detect(frame)

