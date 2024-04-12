using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TagWrapper
{
    public static TagWrapper Create(ArraySegment<double> markerSeg)
    {
        return new TagWrapper(markerSeg);
    }

    private double _markerType;
    private double _markerId;
    private Vector3 _center;
    private Vector3 _xEdge;
    private Vector3 _yEdge;
    private Vector3 _zEdge;
    private Vector3 _centerPos;

    public double MarkerType
    {
        get
        {
            return _markerType;
        }
    }

    public double MarkerId
    {
        get
        {
            return _markerId;
        }
    }

    public Vector3 CenterPos
    {
        get
        {
            return _centerPos;
        }
    }

    public Vector3 XEdge
    {
        get
        {
            return _xEdge;
        }
    }

    public Vector3 YEdge
    {
        get
        {
            return _yEdge;
        }
    }

    public Vector3 ZEdge
    {
        get
        {
            return _zEdge;
        }
    }

    public Vector3 Up
    {
        get
        {
            return _yEdge - _centerPos;
        }
    }

    public Vector3 Forward
    {
        get
        {
            return _zEdge - _centerPos;
        }
    }

    private TagWrapper(ArraySegment<double> markerSeg)
    {
        var marker = markerSeg.Array;

        _markerType = marker[markerSeg.Offset];
        _markerId = marker[markerSeg.Offset + 1];
        _centerPos = ExtractVector3(marker, markerSeg.Offset + 2);
        _xEdge = ExtractVector3(marker, markerSeg.Offset + 5);
        _yEdge = ExtractVector3(marker, markerSeg.Offset + 8);
        _zEdge = ExtractVector3(marker, markerSeg.Offset + 11);
    }

    private Vector3 ExtractVector3(double[] contents, int startIndex)
    {
        if ((startIndex + 2) >= contents.Length || startIndex < 0)
        {
            Debug.Log("Array access out of range");

            return Vector3.zero;
        }

        return new Vector3(
            (float)(contents[startIndex]),
            -(float)(contents[startIndex + 1]),
            (float)(contents[startIndex + 2]));
    }
}
