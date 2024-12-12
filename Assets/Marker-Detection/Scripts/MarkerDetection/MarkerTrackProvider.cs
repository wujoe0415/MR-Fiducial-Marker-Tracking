using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum ArucoTagType
{
    Aruco4x4,
    //Aruco5x5,
    Aruco6x6,
    //Aruco7x7
}

public class MarkerTrackProvider : MonoBehaviour
{
    [HideInInspector]
    public bool IsTracked = false;

    [Tooltip("Call during the frame the tag is tracked")]
    public UnityEvent OnTrack;
    [Tooltip("Call during the frame the tag is not tracked")]
    public UnityEvent OnLoseTrack;

    public void OnTrackTransform(Vector3 position, Quaternion rotation)
    {
        transform.position = position + rotation * (-OffsetPosition);
        transform.rotation = rotation * Quaternion.Inverse(Quaternion.Euler(OffsetRotation));
    }
    public void OnUpdateTrack()
    {
        IsTracked = !IsTracked;
        if (IsTracked)
            OnTrack.Invoke();
        else
            OnLoseTrack.Invoke();
    }
    public ArucoTagType MarkerType = ArucoTagType.Aruco4x4;
    [HideInInspector]
    public Texture2D PreviewSprite;
    [Header("Aruco Tag Transform")]
    public Vector3 OffsetPosition = Vector3.zero;
    public Vector3 OffsetRotation = Vector3.zero;
    private float _tagSize = 0f;

    [Range(0, 20)]
    public int MarkerID = 0;

    public void OnDrawGizmos()
    {
        var rotationMatrix = Matrix4x4.TRS(transform.position + transform.rotation * OffsetPosition, transform.rotation * Quaternion.Euler(OffsetRotation), new Vector3(1, 1, 1));
#if UNITY_EDITOR
        if (!Application.isPlaying)
            Gizmos.color = new Color(1, 1, 0, 0.5f);
        else if (IsTracked)
            Gizmos.color = Color.green;
        else
            Gizmos.color = Color.red;
#endif
        Gizmos.matrix = rotationMatrix;
        _tagSize = 0.069f;
        if (MarkerType == ArucoTagType.Aruco4x4)
            _tagSize /= 2;
#if UNITY_EDITOR
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(_tagSize, _tagSize, 0f));
        if (Application.isPlaying)
            return;
        Gizmos.color = Color.black;
        Gizmos.DrawCube(Vector3.zero, new Vector3(_tagSize, _tagSize, 0f));
        Gizmos.color = Color.white;
        Gizmos.DrawCube(Vector3.zero, new Vector3(_tagSize * 0.8f, _tagSize * 0.8f, 0f));

        Gizmos.color = Color.grey;
        Gizmos.DrawCube(Vector3.right * _tagSize * 0.275f + Vector3.up * _tagSize * 0.275f, new Vector3(_tagSize * 0.2f, _tagSize * 0.2f, 0f));
#endif
    }
}
