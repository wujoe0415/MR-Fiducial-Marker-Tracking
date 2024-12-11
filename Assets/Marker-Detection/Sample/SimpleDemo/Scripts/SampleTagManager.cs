using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MarkerPredictionReciver))]
public class SampleTagManager : MonoBehaviour
{
    private Transform _calibRT;
    private MarkerVisualizer _visualizer;

    private void Start()
    {
        GameObject calib = new GameObject("RT");
        if (FindObjectOfType<OVRCameraRig>() == null)
        {
#if UNITY_EDITOR
            Debug.LogError("Can't find OVR Camera Rig!");
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        calib.transform.parent = GameObject.Find("CenterEyeAnchor").transform;
        calib.AddComponent<ApplyCalibration>();
        _calibRT = calib.transform;
        _visualizer = MarkerVisualizer.Create(_calibRT);

        GetComponent<MarkerPredictionReciver>().AddTagRecieve(UpdateTags);
    }

    public void UpdateTags(double[] markers)
    {
        Debug.Log(markers);
        _visualizer.UpdateTagRoot(_calibRT);
        _visualizer.UpdateTags(markers);
    }
}
