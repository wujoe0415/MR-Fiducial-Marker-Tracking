# MR-Fiducial-Marker-Tracking

[Demo Video]()

**MR-Fiducial-Marker-Tracking** is a Unity package providing a mixed-reality interaction method using [ArUco markers](https://docs.opencv.org/5.x/d5/dae/tutorial_aruco_detection.html). First, we create a 3D model to combine an external camera with Quest 2 or Quest 3. By attaching the camera, we detect the ArUco marker transform information when receiving video images. Following that, we calibrate and retransform the camera coordination to Hmd's one. In this way, objects in the real world can be easily tracked and projected into the virtual world cooridination.


## Requirements
+ Unity 2021.3.26f1 +
+ Meta XR SDK
## Setup

### Python Environment

1. Install Anaconda and create a development environment with [`AnacondaEnvironment.yaml`](https://github.com/wujoe0415/MR-Fiducial-Marker-Tracking/tree/main/MarkerDetectionServer) file.

   `conda env create -f AnacondaEnvironment.yaml`

2. Rename the environment with `IDVR23`.

3. If there are more cameras connected to your computer, you need to change the number of `"camera"{"port":0}` parameter in [`setting.json`](https://github.com/wujoe0415/MR-Fiducial-Marker-Tracking/blob/main/Assets/Resources/aruco_detection/setting.json).

### 3D Model

1. Set up Hmd and controller external model.
2. Attach 4x4 marker id = 0 ArUco on the controller plane.

## Quick Start

We can explore 4 scenes under the Sample folder.

### Calibration

This scene calibrates Hmd and camera coordination. 

1. Make sure your Hmd is placed horizontally on a flat plane.
2. Press K on a keyboard or the right controller trigger to collect position data. 
3. Upon completing data collection and popping up "Calibration Success", the system calculates the homography matrix and automatically calibrates the virtual plane with the tracked controller center.
4. The system then stores the homography matrix into [`matrix.json`](https://github.com/wujoe0415/MR-Fiducial-Marker-Tracking/blob/main/Assets/Marker-Detection/matrix.json), so that you will not need to redo the calibration in each test and build.

### DetectTransform/Army

We provide a high-level ArUco tracking system for quick setup.

1. Create an empty object named `ArucoManager`.
2. Attach the `ArucoDetectionProvider.cs` component. It will be accompanied by `MarkerPredictionRecevier.cs` and `AnancondaStarter.cs`.
3. Make sure the `AnacondaEnvironment` parameter is the same as your name of the environment.
4. Add `TagTrackProvide.cs` on the virtual object you want to track and adjust those parameters.
5. Done！

### SimpleDemo

You are willing to customize the system. We provide the most basic code to implement the ArUco marker tracking system.

+ [`SampleTagManager.cs`](https://github.com/wujoe0415/MR-Fiducial-Marker-Tracking/blob/main/Assets/Marker-Detection/Sample/SimpleDemo/Scripts/SampleTagManager.cs) shows the logic basis of the ArUco tracking system. You may refer to it and develop your system. 

## Contribution

