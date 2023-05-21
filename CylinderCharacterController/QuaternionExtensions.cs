using UnityEngine;

namespace CylinderCharacterController
{
    public static class QuaternionExtensions
    {
        /// <summary>
        /// Fixed version of Quaternion.FromToRotation()
        /// </summary>
        static public Quaternion FromToRotation(Vector3 dir1, Vector3 dir2, Quaternion whenOppositeVectors = default(Quaternion))
        {
            float r = 1f + Vector3.Dot(dir1, dir2);

            if (r < 1E-6f)
            {
                if (whenOppositeVectors == default(Quaternion))
                {
                    // simply get the default behavior
                    return Quaternion.FromToRotation(dir1, dir2);
                }
                return whenOppositeVectors;
            }

            Vector3 w = Vector3.Cross(dir1, dir2);
            return new Quaternion(w.x, w.y, w.z, r).normalized;
        }
    }
}