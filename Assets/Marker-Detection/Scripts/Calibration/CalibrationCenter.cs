
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CalibrationCenter : MonoBehaviour
{
    public enum Device { Quest2, Quest3 }
    public Device Controller = Device.Quest2;
    private Vector3d _topLeft = new Vector3d(-0.0717757f, 0.1340075f, 0.04525972f);
    private Vector3d _topRight = new Vector3d(0.02517137f, 0.1323007f, 0.06983702f);
    private Vector3d _bottomLeft = new Vector3d(-0.05478766f, 0.06652871f, -0.02652233f);
    private Vector3d _bottomRight = new Vector3d(0.04214989f, 0.06478038f, -0.002005272f);
    private class Vector3d
    {
        double x = 0d, y = 0d, z = 0d;

        public Vector3d(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector3d()
        {
            this.x = 0d;
            this.y = 0d;
            this.z = 0d;
        }

        public Vector3 ToVector()
        {
            return new Vector3((float)x, (float)y, (float)z);
        }
        // Addition of two vectors
        public static Vector3d operator +(Vector3d v1, Vector3d v2)
        {
            return new Vector3d(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z);
        }

        // Subtraction of two vectors
        public static Vector3d operator -(Vector3d v1, Vector3d v2)
        {
            return new Vector3d(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z);
        }

        // Scalar multiplication
        public static Vector3d operator *(Vector3d v, double scalar)
        {
            return new Vector3d(v.x * scalar, v.y * scalar, v.z * scalar);
        }

        // Scalar division
        public static Vector3d operator /(Vector3d v, double scalar)
        {
            if (scalar == 0)
            {
                throw new ArgumentException("Cannot divide by zero.");
            }
            return new Vector3d(v.x / scalar, v.y / scalar, v.z / scalar);
        }

        // Dot product of two vectors
        public static double Dot(Vector3d v1, Vector3d v2)
        {
            return v1.x * v2.x + v1.y * v2.y + v1.z * v2.z;
        }

        // Cross product of two vectors
        public static Vector3d Cross(Vector3d v1, Vector3d v2)
        {
            return new Vector3d(
                v1.y * v2.z - v1.z * v2.y,
                v1.z * v2.x - v1.x * v2.z,
                v1.x * v2.y - v1.y * v2.x
            );
        }

        // Magnitude (length) of the vector
        public double Magnitude()
        {
            return Math.Sqrt(x * x + y * y + z * z);
        }

        // Normalize the vector
        public Vector3d Normalize()
        {
            double magnitude = Magnitude();
            if (magnitude == 0)
            {
                throw new InvalidOperationException("Cannot normalize a zero-length vector.");
            }
            return this / magnitude;
        }

        // ToString method for easy printing
        public override string ToString()
        {
            return $"({x}, {y}, {z})";
        }
    }

    private void Awake()
    {
        if(Controller == Device.Quest3)
        {
            _bottomLeft = new Vector3d(0.0160917491d, 0.00959766284d, -0.0928313658d);
            _bottomRight = new Vector3d(0.109332196d, -0.0212159865d, -0.0739435703d);
            _topLeft = new Vector3d(0.0160822552d, 0.0618085563d, -0.00755501958d);
            _topRight = new Vector3d(0.10929998d, 0.0309733469d, 0.0113575244d);
        }
        transform.localPosition = ((_bottomLeft + _bottomRight + _topLeft + _topRight) / 4.0d).ToVector();
    }

}
