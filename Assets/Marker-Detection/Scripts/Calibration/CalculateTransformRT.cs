using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System.IO;

public class CalculateTransformRT : MonoBehaviour
{
    DenseMatrix Rt;

    public void CalculateHomography(Vector3[] sourcePoints, Vector3[] destinationPoints)
    {
        Rt = FindRT2(sourcePoints, destinationPoints);
        CreateFollowerObject(Rt);
    }

    private DenseMatrix FindRT2(Vector3[] sourcePoints, Vector3[] destinationPoints)
    {
        var denseMat = DenseMatrix.Create(3, 4, 0);
        var sMat = DenseMatrix.Create(sourcePoints.Length, 3, 0);
        var dMat = DenseMatrix.Create(destinationPoints.Length, 3, 0);
        var sCenter = CreateVector.Dense<double>(new double[3]);
        var dCenter = CreateVector.Dense<double>(new double[3]);


        // create the matrix
        SetParameters(sourcePoints, ref sMat, ref sCenter);
        SetParameters(destinationPoints, ref dMat, ref dCenter);

        // 
        var hsvd = sMat.Transpose().Multiply(dMat).Transpose().Svd();
        var R = hsvd.VT.Transpose().Multiply(hsvd.U.Transpose()).Transpose();

        if(R.Determinant() < 0)
        {
            var rsvd = R.Svd();
            var rVT = rsvd.VT;
            rVT.SetRow(2, rVT.Row(2).Multiply(-1));
            R = rVT.Transpose().Multiply(rsvd.U.Transpose()).Transpose();
        }

        denseMat.SetSubMatrix(0, 0, R);
        denseMat.SetColumn(3, 0, 3, dCenter - R.Multiply(sCenter));

        return denseMat;
    }

    private void SetParameters(Vector3[] vecs, ref DenseMatrix mat, ref Vector<double> vec)
    {
        for (int i = 0; i < vecs.Length; i++)
            mat.SetRow(i, 0, 3, CreateVector.Dense<double>(new double[] { vecs[i].x, vecs[i].y, vecs[i].z }));

        for(int i = 0; i < 3; i++)
        {
            vec[i] = mat.Column(i).Sum() / vecs.Length;
            mat.SetColumn(i, mat.Column(i).Subtract(vec[i]));
        }
    }

    private void CreateFollowerObject(DenseMatrix rt)
    {
        Debug.Log(rt);
        if(GameObject.Find("RT") != null)
        GameObject.Find("RT").transform.localRotation = Quaternion.Euler(
            Mathf.Atan2(-(float)rt[1, 2], Mathf.Sqrt(1 - Mathf.Pow((float)rt[1, 2], 2))) * Mathf.Rad2Deg,
            Mathf.Atan2((float)rt[0, 2], (float)rt[2, 2]) * Mathf.Rad2Deg,
            Mathf.Atan2((float)rt[1, 0], (float)rt[1, 1]) * Mathf.Rad2Deg);
        GameObject.Find("RT").transform.localPosition = new Vector3((float)rt[0, 3], (float)rt[1, 3], (float)rt[2, 3]);

        SerializableDenseMatrix serializableRt = new SerializableDenseMatrix(rt);

        string json = JsonUtility.ToJson(serializableRt);
        string filePath = "Assets/ArUco/matrix.json";

        if (!File.Exists(filePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.Create(filePath).Close();
        }
        File.WriteAllText(filePath, json);
    }
    
}
[System.Serializable]
public class SerializableDenseMatrix
{
    // You can customize this class based on the structure of DenseMatrix
    public double[] data;
    public int RowCount = 0;
    public int ColumnCount = 0;

    public SerializableDenseMatrix(DenseMatrix matrix)
    {
        // Convert DenseMatrix to a serializable format (e.g., a 2D array)
        RowCount = matrix.RowCount;
        ColumnCount = matrix.ColumnCount;
        int size = matrix.RowCount * matrix.ColumnCount;
        data = new double[size];

        for (int i = 0; i < matrix.RowCount; i++)
        {
            for (int j = 0; j < matrix.ColumnCount; j++)
            {
                data[i * matrix.ColumnCount + j] = matrix[i, j];
            }
        }
    }

    public DenseMatrix ToDenseMatrix()
    {
        // Convert back to DenseMatrix
        DenseMatrix rt = new DenseMatrix(RowCount, ColumnCount);
        for (int i = 0; i < rt.RowCount; i++)
        {
            for (int j = 0; j < rt.ColumnCount; j++)
            {
                rt[i, j] = data[i * rt.ColumnCount + j];
            }
        }
        return rt;
    }
}