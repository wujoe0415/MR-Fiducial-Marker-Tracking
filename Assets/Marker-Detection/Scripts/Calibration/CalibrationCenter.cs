
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vector3d
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
public class DeviceOffset
{ 
    public Vector3d BottomLeft { get; set; }
    public Vector3d BottomRight { get; set; }
    public Vector3d TopLeft { get; set; }
    public Vector3d TopRight { get; set; }
    public DeviceOffset()
    {
        BottomLeft = new Vector3d();
        BottomRight = new Vector3d();
        TopLeft = new Vector3d();
        TopRight = new Vector3d();
    }
    public Vector3 GetCenter()
    {
        return ((BottomLeft + BottomRight + TopLeft + TopRight) / 4.0d).ToVector();
    }
}

public class CalibrationCenter : MonoBehaviour
{
    public enum Device { Quest2, Quest3 }
    public Device HMD = Device.Quest3;

    private void Awake()
    {
        transform.localPosition = GetDeviceOffset(HMD).GetCenter();
    }
    public DeviceOffset GetDeviceOffset(Device hmd)
    {
        if (hmd == Device.Quest2)
        {
            return new DeviceOffset
            {
                BottomLeft = new Vector3d(-0.0717757d, 0.1340075d, 0.04525972d),
                BottomRight = new Vector3d(0.02517137f, 0.1323007f, 0.06983702d),
                TopLeft = new Vector3d(-0.05478766d, 0.06652871d, -0.02652233d),
                TopRight = new Vector3d(0.04214989d, 0.06478038d, -0.002005272d)
            };
        }
        else if (hmd == Device.Quest3)
        {
            return new DeviceOffset
            {
                BottomLeft = new Vector3d(0.0160917491d, 0.00959766284d, -0.0928313658d),
                BottomRight = new Vector3d(0.109332196d, -0.0212159865d, -0.0739435703d),
                TopLeft = new Vector3d(0.0160822552d, 0.0618085563d, -0.00755501958d),
                TopRight = new Vector3d(0.10929998d, 0.0309733469d, 0.0113575244d)
            };
        }
        else
        {
            return new DeviceOffset();
        }
    }
}
