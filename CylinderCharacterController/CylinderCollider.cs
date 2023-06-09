using UnityEngine;

namespace CylinderCharacterController
{
    public class CylinderCollider : MonoBehaviour
    {
        private static Quaternion DefaultDownQuaternion = Quaternion.Euler(0, 180, 0);

        [SerializeField, Tooltip("Height of the Cylinder in units")]
        private float height;
        [SerializeField, Tooltip("Radious of the Cylinder in units")]
        private float radius;
        [SerializeField, Tooltip("Distance Between the ray's origins and the border of the collider")]
        private float skinDepth;
        [Space]
        [SerializeField, Tooltip("Number of vertical layers of the horizontal ray array")]
        private int verticalPointsCount = 3;
        [SerializeField, Tooltip("Number of rays per layer of the horizontal ray array")]
        private int horizontalPointCount = 3;
        [SerializeField, Tooltip("Number of rings of the vertical ray array")]
        private int verticalRings = 2;
        [Space]
        [SerializeField]
        private LayerMask collisionMask;

        private Vector3[,] shapePoints; //[horizontal][vertical]

        private Vector3[,] forwardPoints; //[horizontal][vertical]
        private Ray[,] forwardRays;
        private Quaternion lastHorizontalRotation;
        private RaycastHit[] lastHorizontalHits;
        private float lastHorizontalSpeed;
        private int horizontalHitCount;

        private Vector3[] verticalPoints;
        private Ray[] verticalRays;
        private RaycastHit[] lastVerticalHits;
        private float lastVerticalSpeed;
        private int verticalHitCount;


        private float SkinlessRadius => radius - skinDepth;
        private float SkinlessHeight => height - skinDepth * 2;
        private int RadiusPointCount => horizontalPointCount * 2 - 2;
        private float HorizontalAngleDistance => (360 / RadiusPointCount);
        private int VerticalPointsCount
        {
            get
            {
                int count = 0;
                for (int i = 1; i < verticalRings + 1; i++) count += RadiusPointCount / i;
                return count;
            }
        }
        public RaycastHit[] LastHorizontalHits => lastHorizontalHits;
        public RaycastHit[] LastVerticalHits => lastVerticalHits;

        private void Awake()
        {
            GeneratePointPositions();
        }

        private void GeneratePointPositions()
        {
            GenerateShapePoints();
            GenerateForwardPoints();
            GenerateVerticalPoints();
        }

        private void GenerateShapePoints()
        {
            shapePoints = new Vector3[RadiusPointCount, verticalPointsCount];
            for (int j = 0; j < verticalPointsCount; j++)
            {
                for (int i = 0; i < RadiusPointCount; i++)
                {
                    shapePoints[i, j] =
                        Quaternion.AngleAxis(HorizontalAngleDistance * i, Vector3.up) * Vector3.forward * radius +
                        Vector3.up * (height / (verticalPointsCount - 1)) * j;
                }
            }
        }

        private void GenerateForwardPoints()
        {
            forwardPoints = new Vector3[horizontalPointCount, verticalPointsCount];
            forwardRays = new Ray[horizontalPointCount, verticalPointsCount];
            lastHorizontalHits = new RaycastHit[horizontalPointCount * verticalPointsCount];

            int halfCount = Mathf.FloorToInt(horizontalPointCount * 0.5f);
            Vector3 startingPoint = Quaternion.AngleAxis(HorizontalAngleDistance * -halfCount, Vector3.up) * Vector3.forward * SkinlessRadius;

            for (int j = 0; j < verticalPointsCount; j++)
            {
                for (int i = 0; i < horizontalPointCount; i++)
                {
                    forwardPoints[i, j] =
                        Quaternion.AngleAxis(HorizontalAngleDistance * i, Vector3.up) * startingPoint +
                        Vector3.up * ((SkinlessHeight / (verticalPointsCount - 1)) * j + skinDepth);
                    forwardRays[i, j] = new Ray();
                }
            }
        }

        private void GenerateVerticalPoints()
        {
            verticalPoints = new Vector3[VerticalPointsCount];
            verticalRays = new Ray[verticalPoints.Length];
            lastVerticalHits = new RaycastHit[verticalRays.Length];

            float sectionRadius = SkinlessRadius / verticalRings;

            int pointCount = 0;
            for (int j = 0; j < verticalRings; j++)
            {
                int radiusPoints = RadiusPointCount / (j + 1);
                float radius = sectionRadius * (verticalRings - j);
                float angleDistance = (360 / radiusPoints);
                Vector3 startingPoint = Vector3.forward * radius;
                for (int i = 0; i < radiusPoints; i++)
                {
                    verticalPoints[pointCount] =
                        Quaternion.AngleAxis(angleDistance * i, Vector3.up) * startingPoint +
                        Vector3.up * skinDepth;
                    verticalRays[pointCount] = new Ray();
                    pointCount++;
                }
            }
        }

        /// <summary>
        /// Check for collision by casting rays horizontally
        /// </summary>
        public int CheckHorizontalCollision(Vector3 velocity)
        {
            return CheckHorizontalCollision(velocity, Vector3.zero);
        }

        /// <summary>
        /// Check for collision by casting rays horizontally with the center of the collider displaced
        /// </summary>
        public int CheckHorizontalCollision(Vector3 velocity, Vector3 displacement)
        {
            Vector3 position = transform.position + displacement;
            lastHorizontalRotation = QuaternionExtensions.FromToRotation(Vector3.forward, Vector3.ProjectOnPlane(velocity.normalized, Vector3.up), DefaultDownQuaternion);
            lastHorizontalSpeed = velocity.magnitude + skinDepth;
            horizontalHitCount = 0;

            for (int j = 0; j < verticalPointsCount; j++)
            {
                for (int i = 0; i < horizontalPointCount; i++)
                {
                    forwardRays[i, j].origin = position + lastHorizontalRotation * forwardPoints[i, j];
                    forwardRays[i, j].direction = velocity.normalized;
                    if (Physics.Raycast(forwardRays[i, j], out lastHorizontalHits[horizontalHitCount], lastHorizontalSpeed, collisionMask))
                    {
                        bool replace = false;
                        lastHorizontalHits[horizontalHitCount].distance -= skinDepth;
                        if (lastHorizontalHits[horizontalHitCount].distance < 0) lastHorizontalHits[horizontalHitCount].distance = 0;
                        for (int k = 0; k < horizontalHitCount; k++)
                        {
                            if (lastHorizontalHits[horizontalHitCount].colliderInstanceID == lastHorizontalHits[k].colliderInstanceID)
                            {
                                if (lastHorizontalHits[horizontalHitCount].distance < lastHorizontalHits[k].distance)
                                    lastHorizontalHits[k] = lastHorizontalHits[horizontalHitCount];
                                replace = true;
                                break;
                            }
                        }

                        if (!replace) horizontalHitCount++;
                    }
                }
            }

            return horizontalHitCount;
        }

        /// <summary>
        /// Check for collision by casting rays Vertically
        /// </summary>
        public int CheckVerticalCollision(float distance)
        {
            return CheckVerticalCollision(distance, Vector3.zero);
        }

        /// <summary>
        /// Check for collision by casting rays Vertically with the center of the collider displaced
        /// </summary>
        public int CheckVerticalCollision(float distance, Vector3 displacement)
        {
            if (distance < 0) return CheckVerticalDownCollision(distance, displacement);
            else if (distance > 0) return CheckVerticalUpCollision(distance, displacement);
            else return 0;
        }

        private int CheckVerticalDownCollision(float distance, Vector3 displacement)
        {
            Vector3 position = transform.position + displacement;
            lastVerticalSpeed = Mathf.Abs(distance) + skinDepth;
            verticalHitCount = 0;
            for (int i = 0; i < verticalRays.Length; i++)
            {
                verticalRays[i].origin = position + verticalPoints[i];
                verticalRays[i].direction = Vector3.down;
                if (Physics.Raycast(verticalRays[i], out lastVerticalHits[verticalHitCount], lastVerticalSpeed, collisionMask))
                {
                    bool replace = false;
                    lastVerticalHits[verticalHitCount].distance -= skinDepth;
                    if (lastVerticalHits[verticalHitCount].distance < 0) lastVerticalHits[verticalHitCount].distance = 0;
                    for (int k = 0; k < verticalHitCount; k++)
                    {
                        if (lastVerticalHits[verticalHitCount].colliderInstanceID == lastVerticalHits[k].colliderInstanceID)
                        {
                            if (lastVerticalHits[verticalHitCount].distance < lastVerticalHits[k].distance)
                                lastVerticalHits[k] = lastVerticalHits[verticalHitCount];
                            replace = true;
                            break;
                        }
                    }

                    if (!replace) verticalHitCount++;
                }
            }

            return verticalHitCount;
        }

        private int CheckVerticalUpCollision(float speed, Vector3 displacement)
        {
            Vector3 position = transform.position + displacement;
            lastVerticalSpeed = Mathf.Abs(speed) + skinDepth;
            verticalHitCount = 0;
            Vector3 height = Vector3.up * SkinlessHeight;
            for (int i = 0; i < verticalRays.Length; i++)
            {
                verticalRays[i].origin = position + verticalPoints[i] + height;
                verticalRays[i].direction = Vector3.up;
                if (Physics.Raycast(verticalRays[i], out lastVerticalHits[verticalHitCount], lastVerticalSpeed, collisionMask))
                {
                    bool replace = false;
                    lastVerticalHits[verticalHitCount].distance -= skinDepth;
                    if (lastVerticalHits[verticalHitCount].distance < 0) lastVerticalHits[verticalHitCount].distance = 0;
                    for (int k = 0; k < verticalHitCount; k++)
                    {
                        if (lastVerticalHits[verticalHitCount].colliderInstanceID == lastVerticalHits[k].colliderInstanceID)
                        {
                            if (lastVerticalHits[verticalHitCount].distance < lastVerticalHits[k].distance)
                                lastVerticalHits[k] = lastVerticalHits[verticalHitCount];
                            replace = true;
                            break;
                        }
                    }

                    if (!replace) verticalHitCount++;
                }
            }

            return verticalHitCount;
        }

        public RaycastHit GetClosestVerticalHit()
        {
            bool assigned = false;
            RaycastHit hit = new RaycastHit();
            for (int i = 0; i < verticalHitCount; i++)
            {
                if (!assigned)
                {
                    hit = LastVerticalHits[i];
                    assigned = true;
                }

                if (hit.distance > LastVerticalHits[i].distance)
                    hit = LastVerticalHits[i];
            }

            return hit;
        }


        public RaycastHit GetClosestHorizontalHit()
        {
            bool assigned = false;
            RaycastHit hit = new RaycastHit();
            for (int i = 0; i < horizontalHitCount; i++)
            {
                if (!assigned)
                {
                    hit = LastHorizontalHits[i];
                    assigned = true;
                }

                if (hit.distance > LastHorizontalHits[i].distance)
                    hit = LastHorizontalHits[i];
            }

            return hit;
        }

        private void OnValidate()
        {
            GeneratePointPositions();
        }

        private void OnDrawGizmos()
        {
            Vector3 position = transform.position;
            //Draw Shape
            Gizmos.color = Color.green;
            for (int j = 0; j < verticalPointsCount; j++)
            {
                for (int i = 0; i < RadiusPointCount; i++)
                {
                    Vector3 thisPosition = position + shapePoints[i, j];
                    Vector3 nextPosition = position + shapePoints[(i + 1) % RadiusPointCount, j];

                    Gizmos.DrawSphere(thisPosition, 0.025f);
                    Gizmos.DrawLine(thisPosition, nextPosition);
                    if (j > 0)
                    {
                        Vector3 downPosition = position + shapePoints[i, j - 1];
                        Gizmos.DrawLine(thisPosition, downPosition);
                    }
                }
            }

            //draw horizontal rays
            Gizmos.color = Color.red;
            for (int j = 0; j < verticalPointsCount; j++)
            {
                for (int i = 0; i < horizontalPointCount; i++)
                {
                    Gizmos.DrawSphere(forwardRays[i, j].origin, 0.01f);
                    Gizmos.DrawLine(forwardRays[i, j].origin, forwardRays[i, j].origin + forwardRays[i, j].direction * lastHorizontalSpeed);
                }
            }


            //draw horizontal rays
            Gizmos.color = Color.blue;
            for (int i = 0; i < verticalRays.Length; i++)
            {
                Gizmos.DrawSphere(verticalRays[i].origin, 0.01f);
                Gizmos.DrawLine(verticalRays[i].origin, verticalRays[i].origin + verticalRays[i].direction * lastVerticalSpeed);
            }
        }
    }
}