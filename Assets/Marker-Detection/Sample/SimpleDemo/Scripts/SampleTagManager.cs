using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TagPredictionReciver))]
public class SampleTagManager : MonoBehaviour
{
    private Transform _calibRT;
    private TagVisualizer _visualizer;

    private void Start()
    {
        GameObject calib = new GameObject("RT");
        if (GameObject.Find("OVRCameraRig") == null)
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
        _visualizer = TagVisualizer.Create(_calibRT);

        GetComponent<TagPredictionReciver>().AddTagRecieve(UpdateTags);
    }

    public void UpdateTags(double[] markers)
    {
        Debug.Log(markers);
        _visualizer.UpdateTagRoot(_calibRT);
        _visualizer.UpdateTags(markers);
    }
}
