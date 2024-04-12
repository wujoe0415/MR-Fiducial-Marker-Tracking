using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TagVisualizer
{
    private Transform _tagRootRT;
    private List<GameObject> _tagPrefabs;

    private TagVisualizer(Transform rt)
    {
        _tagRootRT = rt;
        _tagPrefabs = new List<GameObject>();
    }

    public static TagVisualizer Create(Transform rt)
    {
        return new TagVisualizer(rt);
    }

    private void ResetUnusedPrefab(int startIndex)
    {
        for (int i = startIndex; i < _tagPrefabs.Count; i++)
        {
            _tagPrefabs[i].SetActive(false);
        }
    }

    public void UpdateTagRoot(Transform calibrateTransform)
    {

        _tagRootRT.localRotation = calibrateTransform.localRotation;
        _tagRootRT.localPosition = calibrateTransform.localPosition;
        _tagRootRT.localScale = Vector3.one;
    }

    public void UpdateTags(double[] markers)
    {
        var markerCounts = markers[0];

        for (int i = 0; i < markerCounts; i++)
        {
            UpdateTag(i, new ArraySegment<double>(markers, i * 14 + 1, 14));
        }

        if (markerCounts == _tagPrefabs.Count)
        {
            return;
        }

        ResetUnusedPrefab((int)markerCounts);
    }

    private void UpdateTag(int prefabNum, ArraySegment<double> markerSeg)
    {
        var tagWrapper = TagWrapper.Create(markerSeg);
        
        while (_tagPrefabs.Count <= prefabNum)
        {
            var tagPrefab = CreatePrefab();

            tagPrefab.transform.parent = _tagRootRT;
            tagPrefab.transform.localScale = Vector3.one * 0.01f;
            _tagPrefabs.Add(tagPrefab);
        }

        _tagPrefabs[prefabNum].transform.localPosition = tagWrapper.CenterPos;
        _tagPrefabs[prefabNum].transform.localRotation = 
            Quaternion.LookRotation(
                tagWrapper.Forward,
                tagWrapper.Up);
        _tagPrefabs[prefabNum].SetActive(true);
    }

    private GameObject CreatePrefab()
    {
        GameObject CreateCube(Color color, Vector3 pos, Vector3 scale)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);

            obj.GetComponent<Renderer>().material.color = color;
            obj.transform.localPosition = pos;
            obj.transform.localScale = scale;

            return obj;
        }
        float axisLength = 2.5f;
        var tagPrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var xAxis = CreateCube(
            new Color(1, 0, 0, 1),
            new Vector3(0.5f * axisLength, 0.0f, 0.0f),
            new Vector3(1f * axisLength, 0.1f, 0.1f));
        var yAxis = CreateCube(
            new Color(0, 1, 0, 1),
            new Vector3(0.0f, 0.5f * axisLength, 0.0f),
            new Vector3(0.1f, 1f * axisLength, 0.1f));
        var zAxis = CreateCube(
            new Color(0, 0, 1, 1),
            new Vector3(0.0f, 0.0f, 0.5f * axisLength),
            new Vector3(0.1f, 0.1f, 1f * axisLength));
        
        xAxis.transform.parent = tagPrefab.transform;
        yAxis.transform.parent = tagPrefab.transform;
        zAxis.transform.parent = tagPrefab.transform;

        return tagPrefab;
    }
}
