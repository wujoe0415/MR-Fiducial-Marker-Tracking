using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MarkerPredictionReciver))]
public class ArucoDetectionProvider : MonoBehaviour, IDetectionProvider
{
    private Transform _calibRT;
    private List<MarkerTrackProvider> _arucoTagTrackProviders = new List<MarkerTrackProvider>();

    private int _tagTypeNum = 2;
    private List<Dictionary<int, MarkerTrackProvider>> _trackers = new List<Dictionary<int, MarkerTrackProvider>>(); // 0 for 6*6, 1 for 4*4
    
    private IEnumerator _coroutine;

    public bool ShowWireTag = false;
    public bool ShowViewFrustrum = false;
    private MarkerDrawer _drawer;
    private SerializeFrustrum _frustrum;

    [HideInInspector]
    public double[] TagInfo = null;
    private int[] _currentFrameTracked;

    public void AddTracker(MarkerTrackProvider tracker)
    {
        if (tracker.MarkerType == ArucoTagType.Aruco4x4)
        {
            if (_trackers[1].ContainsKey(tracker.MarkerID))
            {
                Debug.LogWarning("Key has bee added!");
                return;
            }
            _trackers[1].Add(tracker.MarkerID, tracker);
        }
        else
        {
            if (_trackers[0].ContainsKey(tracker.MarkerID))
            {
                Debug.LogWarning("Key has bee added!");
                return;
            }
            _trackers[0].Add(tracker.MarkerID, tracker);
        }
        _arucoTagTrackProviders.Add(tracker);
    }
    public void RemoveTracker(MarkerTrackProvider tracker)
    {
        if (tracker.MarkerType == ArucoTagType.Aruco4x4)
        {
            if (_trackers[1].ContainsKey(tracker.MarkerID))
            {
                Debug.LogWarning("Key cannot be found!");
                return;
            }
            _trackers[1].Remove(tracker.MarkerID);
        }
        else
        {
            if (_trackers[0].ContainsKey(tracker.MarkerID))
            {
                Debug.LogWarning("Key cannot be found!");
                return;
            }
            _trackers[0].Remove(tracker.MarkerID);
        }
        _arucoTagTrackProviders.Remove(tracker);
    }

    private void Awake()
    {
        for (int i = 0; i < _tagTypeNum; i++)
            _trackers.Add(new Dictionary<int, MarkerTrackProvider>());

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
        calib.AddComponent<SerializeFrustrum>();
        _frustrum = calib.GetComponent<SerializeFrustrum>();
        _frustrum.enabled = ShowViewFrustrum;
        _calibRT = calib.transform;
    }
    private void Start()
    {
        Shader tmp = Shader.Find("Unlit/Color");
        Material tagMaterial = new Material(tmp);
        tagMaterial.SetColor("_Color", new Color(1f, 195f / 255, 0f));
        _drawer = new MarkerDrawer(tagMaterial);
        
        var _aprilTagTrackInScene = FindObjectsOfType<MarkerTrackProvider>();

        foreach (MarkerTrackProvider track in _aprilTagTrackInScene)
            AddTracker(track);
        GetComponent<MarkerPredictionReciver>().AddTagRecieve(UpdateTagInfo);
    }
    private void OnEnable()
    {
        if (_coroutine != null)
            StopCoroutine(_coroutine);
        _coroutine = UpdateTagsTransform();
        StartCoroutine(_coroutine);
    }
    private void OnDisable()
    {
        _drawer.Dispose();
        _arucoTagTrackProviders.Clear();
        _trackers.Clear();
        if (_coroutine != null)
            StopCoroutine(_coroutine);
        _coroutine = null;
    }
    private void UpdateTag(int prefabNum, ArraySegment<double> tagSig)
    {
        var tagWrapper = MarkerWrapper.Create(tagSig);
        int tagType = (int)tagWrapper.MarkerType;

        if (tagType >= _tagTypeNum || !_trackers[tagType].ContainsKey((int)tagWrapper.MarkerId))
            return;
        
        _currentFrameTracked[tagType] |= 1 << (int)tagWrapper.MarkerId;
        Vector3 pos = tagWrapper.CenterPos;
        Quaternion rot = Quaternion.LookRotation(tagWrapper.Forward.normalized, tagWrapper.Up.normalized);

        pos = _calibRT.position + _calibRT.rotation * pos;
        rot = _calibRT.rotation * rot;
        _trackers[tagType][(int)tagWrapper.MarkerId].OnTrackTransform(pos, rot);

        if (!ShowWireTag)
            return;
        float tagSize = tagType == 0 ? 0.058f : 0.027f;
        _drawer.Draw((int)tagWrapper.MarkerId, pos, rot, tagSize);
    }
    public void UpdateTagInfo(double[] markers)
    {
        // receive from predictor
        TagInfo = markers;
    }
    
    public IEnumerator UpdateTagsTransform()
    {
        _currentFrameTracked = new int[_tagTypeNum];
        int[] lastFrameTracked = new int[_tagTypeNum];
        for (int i = 0; i < _tagTypeNum; i++)
        {
            _currentFrameTracked[i] = 0;
            lastFrameTracked[i] = 0;
        }

        while (true)
        {
            // Serialize View Frustru
            _frustrum.enabled = ShowViewFrustrum;

            if (TagInfo.Length != 0)
            {
                for (int t = 0; t < _tagTypeNum; t++)
                    _currentFrameTracked[t] = 0;

                var markerCounts = TagInfo[0];
                for (int i = 0; i < markerCounts; i++)
                    UpdateTag(i, new ArraySegment<double>(TagInfo, i * 14 + 1, 14));
            }

            // Detect LoseTrack
            for(int t = 0 ; t < _tagTypeNum ; t++)
            {
                int diff = lastFrameTracked[t] ^ _currentFrameTracked[t];
                for (int i = 0; i < 32; i++)
                {
                    if (!_trackers[t].ContainsKey(i))
                        continue;
                    else if ((diff & (1 << i)) != 0)
                        _trackers[t][i].OnUpdateTrack();
                }

                lastFrameTracked[t] = _currentFrameTracked[t];
            }
            yield return null;
        }
    }
}
