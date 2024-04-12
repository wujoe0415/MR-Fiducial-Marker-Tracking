using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(CalculateTransformRT))]
public class MarkerCalibration : MonoBehaviour
{
    [Range(500, 3000)]
    public int SamplePointNumber = 1000;
    private KeyCode _keyBoardCalibration = KeyCode.K;

    public MarkerTrackProvider TrackProvider;
    public Transform TagCamera;
    public Transform VRCalibrationPoint;
    public Transform VRCamera;

    private bool _isCalibration = false;

    private List<Vector3> _realRefPointsList = new List<Vector3>();
    private List<Vector3> _virtualRefPointsList = new List<Vector3>();

    private CalculateTransformRT _transformRT;

    private IEnumerator _coroutine;

    [Header("Screen UI")]
    public TextMeshProUGUI Msg;
    public Image Progress;
    public Gradient ProgressColor;

    private void Start()
    {
        _realRefPointsList.Clear();
        _virtualRefPointsList.Clear();
        Progress.fillAmount = 0f;
        Progress.color = ProgressColor.Evaluate(Progress.fillAmount);

        _transformRT = GetComponent<CalculateTransformRT>();
        Msg.text = "Press Trigger or K to start calibration.";

        if (GameObject.Find("RT") != null)
        {
            Debug.Log("Find RT");
            GameObject rt = GameObject.Find("RT");
            rt.transform.localPosition = Vector3.zero;
            rt.transform.localRotation = Quaternion.identity;
            rt.transform.localScale = new Vector3(1f, 1f, 1f);
        }
    }

    private void Update()
    {
        if (_isCalibration) return;
        if(_coroutine == null && ((OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch) > 0.3f)  || (Input.GetKey(_keyBoardCalibration))))
        {
            Debug.Log("Calibrate");
            Msg.text = "Calibration Status : ";
            _coroutine = Calibration();
            StartCoroutine(_coroutine);
        }
        else if(_coroutine != null && ((OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch)) || (Input.GetKeyUp(_keyBoardCalibration))))
        {
            Debug.Log("Release");
            StopCoroutine(_coroutine);
            _coroutine = null;
            Msg.text = "Press Trigger or K to continue calibration.";
        }
    }
    private void OnDisable()
    {
        if (_coroutine != null)
            StopCoroutine(_coroutine);
        _coroutine = null;
    }
    private IEnumerator Calibration()
    {
        while (_realRefPointsList.ToArray().Length < SamplePointNumber)
        {
            CollectTwoReferencePointsPosition();
            Progress.fillAmount = (float)_realRefPointsList.ToArray().Length / SamplePointNumber;
            Progress.color = ProgressColor.Evaluate(Progress.fillAmount);
            yield return null;
        }
        _transformRT.CalculateHomography(_realRefPointsList.ToArray(), _virtualRefPointsList.ToArray());
        Msg.text = "Calibration Finished!";
        Progress.gameObject.SetActive(false);
        _isCalibration = true;
    }

    private void CollectTwoReferencePointsPosition()
    {
        if (!TrackProvider.IsTracked)
            return;
        _realRefPointsList.Add(Quaternion.Inverse(TagCamera.rotation) * (TrackProvider.transform.position - TagCamera.position));
        _virtualRefPointsList.Add(Quaternion.Inverse(VRCamera.rotation) * (VRCalibrationPoint.position - VRCamera.position));
    }
}
