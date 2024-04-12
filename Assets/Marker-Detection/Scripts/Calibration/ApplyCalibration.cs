using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

public class ApplyCalibration : MonoBehaviour
{
    private void Awake()
    {
        DenseMatrix rt = LoadMatrix();
        if (rt == null)
            return;
        transform.localRotation = Quaternion.Euler(
            Mathf.Atan2(-(float)rt[1, 2], Mathf.Sqrt(1 - Mathf.Pow((float)rt[1, 2], 2))) * Mathf.Rad2Deg,
            Mathf.Atan2((float)rt[0, 2], (float)rt[2, 2]) * Mathf.Rad2Deg,
            Mathf.Atan2((float)rt[1, 0], (float)rt[1, 1]) * Mathf.Rad2Deg);
        transform.localPosition = new Vector3((float)rt[0, 3], (float)rt[1, 3], (float)rt[2, 3]);
    }
    private DenseMatrix LoadMatrix()
    {
        string filePath = "Assets/ArUco/matrix.json";
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            SerializableDenseMatrix serializableRt = JsonUtility.FromJson<SerializableDenseMatrix>(json);
            DenseMatrix rt = serializableRt.ToDenseMatrix();
            return rt;
        }
        else
        {
            Debug.LogError("File not found: " + filePath);
            return null;
        }
    }
}
