using Oculus.Platform.Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlPassthrough : MonoBehaviour
{
    private KeyCode TriggerKey = KeyCode.Space;
    public OVRManager PassThroughControl;
    public Material SettingSkybox;

    private void Start()
    {
        if (PassThroughControl == null)
            PassThroughControl = GameObject.Find("OVRCameraRig").GetComponent<OVRManager>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(TriggerKey))
            ControlPassthroughEnable();
    }

    public void ControlPassthroughEnable()
    {   
        PassThroughControl.isInsightPassthroughEnabled = !PassThroughControl.isInsightPassthroughEnabled;
        RenderSettings.skybox = PassThroughControl.isInsightPassthroughEnabled ? null : SettingSkybox;
    }
}
