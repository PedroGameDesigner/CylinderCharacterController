using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Vector3Extension
{
    public static Vector3 GetVerticalComponent(this Vector3 vector)
    {
        return vector.y * Vector3.up;
    }

    public static Vector3 GetHorizontalComponent(this Vector3 vector)
    {
        return Vector3.ProjectOnPlane(vector, Vector3.up);
    }
}
