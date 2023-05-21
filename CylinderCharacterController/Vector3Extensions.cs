using UnityEngine;

namespace CylinderCharacterController
{
    public static class Vector3Extension
    {
        /// <summary>
        /// Get the Y component of the Vector
        /// </summary>
        public static Vector3 GetVerticalComponent(this Vector3 vector)
        {
            return vector.y * Vector3.up;
        }

        /// <summary>
        /// Get the Vector projected on the X-Z plane
        /// </summary>
        public static Vector3 GetHorizontalComponent(this Vector3 vector)
        {
            return Vector3.ProjectOnPlane(vector, Vector3.up);
        }
    }
}