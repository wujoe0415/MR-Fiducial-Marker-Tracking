# MR-Fiducial-Marker-Tracking

[Demo Video]()

**MR-Fiducial-Marker-Tracking** is a Unity package providing a mixed-reality interaction method using [ArUco markers](https://docs.opencv.org/5.x/d5/dae/tutorial_aruco_detection.html). First, we create a 3D model to combine an external camera with Quest 2 or Quest 3. By attaching the camera, we detect the ArUco marker transform information when receiving video images. Following that, we calibrate and retransform the camera coordination to Hmd's one. In this way, objects in the real world can be easily tracked and projected into the virtual world cooridination.


## Requirements
+ Unity 2021.3.26f1 +
+ Meta XR SDK
## Setup

Before using this plugin, you are required to set up a Python environment and 3D model to help your attached camera self-calibrated.

In our settings, we support the following ArUco markers.
The size of 0.056 cm `DICT_6X6_250`, and the size of 0.027 cm DICT_4X4_250. Please make sure your markers is identical to this specification.

In our model, we use a NexiGo N980P 60fps Webcam for the demo, so we made the camera flip 180 degrees horizontally to fit the model. If you want to develop with your own device, please modify the information in [setting.json](https://github.com/wujoe0415/MR-Fiducial-Marker-Tracking/blob/main/Assets/Resources/aruco_detection/setting.json), especially the `flip_code` property.

### 3D Model

We design [3D model](https://github.com/wujoe0415/MR-Fiducial-Marker-Tracking/tree/main/3D%20Models) and calculate their corresponding coordination as calibration points. 

1. Please refer to 3D model to get the file.
2. Set up Hmd and controller external models.
3. Attach 4x4 marker id = 0 ArUco on the controller plane.

### Camera Calibration

We implement [OpenCV Camera Calibration](https://docs.opencv.org/4.x/dc/dbb/tutorial_py_calibration.html) technology to help us quickly gather device information.

1. Check your camera device's technical specifications in detail, and modify the [setting.json](https://github.com/wujoe0415/MR-Fiducial-Marker-Tracking/blob/main/Assets/Resources/aruco_detection/setting.json) file with the corresponding attributes.

2. Prepare a series of different-angle photos that are shot on a checkerboard, with a pattern size of 9x6 and an edge size of 0.024 cm, with your own camera. Store those images under the [checkerboard_photos](https://github.com/wujoe0415/MR-Fiducial-Marker-Tracking/tree/main/Assets/Resources/aruco_detection/checkerboard_photos) folder.

   You also provide `camera_calibrator.py`, which allows you to execute it and help you take photos.

For example, we are using [NexiGo N980P 60fps Webcam](https://www.hellodirect.com/nexigo-n980p-60fps-webcam) for the demo, which gives 720x1280 resolution and 60 fps image views. Therefore, I change the attributes resolution and fps.

### Python Environment

1. Install Anaconda and create a development environment with [`AnacondaEnvironment.yaml`](https://github.com/wujoe0415/MR-Fiducial-Marker-Tracking/tree/main/MarkerDetectionServer) file.

   `conda env create -f AnacondaEnvironment.yaml`

2. Rename the environment with `IDVR23`.

3. If there are more cameras connected to your computer, you need to change the number of `"camera"{"port":0}` parameter in [`setting.json`](https://github.com/wujoe0415/MR-Fiducial-Marker-Tracking/blob/main/Assets/Resources/aruco_detection/setting.json).

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
5. Done!

### SimpleDemo

You are willing to customize the system. We provide the most basic code to implement the ArUco marker tracking system.

+ [`SampleTagManager.cs`](https://github.com/wujoe0415/MR-Fiducial-Marker-Tracking/blob/main/Assets/Marker-Detection/Sample/SimpleDemo/Scripts/SampleTagManager.cs) shows the logic basis of the ArUco tracking system. You may refer to it and develop your system. 

## Troubleshooting

+ Unity Warning Message: `Anaconda activate.bat, the $path$ file was not found!`

  Please check your Anaconda `activate.bat`(`$path$\anaconda3\Scripts\activate.bat`) file path and refer it to  `_anacondaPath` variable in `AnacondaStarter.cs`.

+ Unity Warning Message: `Fail to find aruco_detection dictionary!`

  Please check whether there is a folder in path  `$Application.persistentDataPath$\aruco_detection`.

+ Unity Warning Message: Fail to find the name of the anaconda environment.
  
  The default Anaconda environment is `aruco_detection`; if you rename it, you must refer to the variable in `AnacondaStarter.cs`.
  

## Contribution

