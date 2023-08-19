using System.Collections.Generic;
using UnityEngine;

namespace CylinderCharacterController
{
    public class CharacterPhysics : MonoBehaviour
    {
        private static float MIN_DIFFERENCE = 0.001f;

        [SerializeField, Tooltip("Maximum angle the character can climb up")]
        private float maxSlopeAngle = 75;
        [SerializeField, Tooltip("Maximum height of the steps that the character can climb or descend automatically")]
        private float totalStepHeight = 0.2f;
        [SerializeField, Tooltip("Number of steps of the collision calculation.")]
        private float stepCount = 4;

        private new CylinderCollider collider;
        private List<CylinderCollider> horizontalSubColliders = new List<CylinderCollider>();
        private List<CylinderCollider> verticalSubColliders = new List<CylinderCollider>();
        private CollisionState ceilingState = new CollisionState();
        private CollisionState floorState = new CollisionState();

        private Vector3 speed;
        public Vector3 Speed { get => speed; set { speed = value;  } }
        public bool IsOnFloor => floorState.colliding == true && 
                                (floorState.slopeState == CollisionState.SlopeState.Flat ||
                                floorState.slopeState == CollisionState.SlopeState.Slope);
        public bool IsTouchingCeiling => ceilingState.colliding == true &&
                                        (ceilingState.slopeState == CollisionState.SlopeState.Flat ||
                                        ceilingState.slopeState == CollisionState.SlopeState.Slope ||
                                        ceilingState.slopeState == CollisionState.SlopeState.StepSlope);

        private void Awake()
        {
            collider = GetComponent<CylinderCollider>();
        }

        void FixedUpdate()
        {
            if (Speed.sqrMagnitude > float.Epsilon)
            {
                float time = Time.fixedDeltaTime;
                ceilingState.Reset();
                floorState.Reset();
                Vector3 translation = Speed * time;
                Vector3 extraTranslation = ProcessVerticalDisplacement(translation.GetVerticalComponent());
                ProcessHorizontalDisplacement(translation.GetHorizontalComponent(), extraTranslation);
            }
        }

        Vector3 ProcessVerticalDisplacement(Vector3 verticalTranslation)
        {
            Vector3 translation = verticalTranslation;
            Vector3 extraTranslation = Vector3.zero;
            int hitCount = CheckVerticalCollision(translation.y);

            if (hitCount > 0)
            {
                RaycastHit closestHit = GetClosestVerticalHits();
                if (translation.y > 0)
                {
                    UpdateCeilingState(closestHit);
                    if (ceilingState.slopeState == CollisionState.SlopeState.Flat || ceilingState.slopeState == CollisionState.SlopeState.Slope)
                        translation = CapTranslationToCollision(translation, closestHit);
                    else if (ceilingState.slopeState == CollisionState.SlopeState.StepSlope)
                    {
                        Vector3 oldTranslation = translation;
                        translation = CapTranslationToCollision(translation, closestHit);
                        extraTranslation = GetSteepSlopeTranslation(oldTranslation - translation, ceilingState);
                    }
                }
                else if (translation.y < 0)
                {
                    UpdateFloorState(closestHit);
                    if (floorState.slopeState == CollisionState.SlopeState.Flat || floorState.slopeState == CollisionState.SlopeState.Slope)
                        translation = CapTranslationToCollision(translation, closestHit);
                    else if (floorState.slopeState == CollisionState.SlopeState.StepSlope)
                    {
                        Vector3 oldTranslation = translation;
                        translation = CapTranslationToCollision(translation, closestHit);
                        extraTranslation = GetSteepSlopeTranslation(oldTranslation - translation, floorState);
                    }
                }
            }

            Translate(translation);
            return extraTranslation;
        }

        private void UpdateCeilingState(RaycastHit hit)
        {
            ceilingState.colliding = true;
            ceilingState.normal = hit.normal;
            float angle = Vector3.Angle(hit.normal, Vector3.down);

            if (angle > maxSlopeAngle) ceilingState.slopeState = CollisionState.SlopeState.StepSlope;
            else if (angle > 0) ceilingState.slopeState = CollisionState.SlopeState.Slope;
            else ceilingState.slopeState = CollisionState.SlopeState.Flat;
        }

        private void UpdateFloorState(RaycastHit hit)
        {
            floorState.colliding = true;
            floorState.normal = hit.normal;
            float angle = Vector3.Angle(hit.normal, Vector3.up);

            if (angle > maxSlopeAngle) floorState.slopeState = CollisionState.SlopeState.StepSlope;
            else if (angle > 0) floorState.slopeState = CollisionState.SlopeState.Slope;
            else floorState.slopeState = CollisionState.SlopeState.Flat;
        }

        private Vector3 CapTranslationToCollision(Vector3 translation, RaycastHit hit)
        {
            return hit.distance * translation.normalized;
        }

        private void ProcessHorizontalDisplacement(Vector3 horizontalTranslation, Vector3 extraTranslation)
        {
            Vector3 translation = SelectHorizontalTranslation(horizontalTranslation, extraTranslation);
            translation = TryClimbSteps(translation);

            Translate(translation.GetHorizontalComponent());

            Vector3 verticalTranslation = SelectVerticalTranslation(translation);            
            ProcessVerticalDisplacement(verticalTranslation);
        }

        private Vector3 TryClimbSteps(Vector3 translation)
        {
            float distance = translation.magnitude;
            float subStep = totalStepHeight / stepCount;

            Vector3 bestHeigth = Vector3.zero;
            float bestDistance = 0;

            for (int i = 0; i < stepCount; i++)
            {
                Vector3 stepHeight = subStep * i * Vector3.up;
                int hitCount = CheckHorizontalCollision(translation, stepHeight);
                if (hitCount <= 0)
                {
                    bestHeigth = stepHeight;
                    bestDistance = distance;
                    break;
                }
                else
                {
                    var hit = GetClosestHorizontalHits();
                    if (MoreThan(hit.distance, bestDistance))
                    {
                        bestHeigth = stepHeight;
                        bestDistance = hit.distance;
                    }
                }
            }
            return translation.normalized * bestDistance + bestHeigth;
        }

        private Vector3 SelectHorizontalTranslation(Vector3 horizontalTranslation, Vector3 extraTranslation)
        {
            CollisionState state;
            if (floorState.colliding)
                state = floorState;
            else
                state = ceilingState;

            switch (state.slopeState)
            {
                case CollisionState.SlopeState.Slope:
                    return GetSlopeTranslation(horizontalTranslation);
                case CollisionState.SlopeState.StepSlope:
                    return extraTranslation;
            }
            return horizontalTranslation;
        }

        private Vector3 GetSlopeTranslation(Vector3 horizontalTranslation)
        {
            Vector3 projectedSlope = Vector3.ProjectOnPlane(floorState.normal, Vector3.Cross(horizontalTranslation, Vector3.up));
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, projectedSlope);

            return rotation * horizontalTranslation;
        }

        private Vector3 GetSteepSlopeTranslation(Vector3 verticalTranslation, CollisionState state)
        {
            Vector3 axis = Vector3.Cross(verticalTranslation, state.normal);
            Quaternion rotation = Quaternion.AngleAxis(90, axis);

            return rotation * state.normal * -Mathf.Abs(verticalTranslation.y);
        }

        private Vector3 SelectVerticalTranslation(Vector3 prevTranslation)
        {
            Vector3 verticalTranslation = prevTranslation.GetVerticalComponent();
            if (floorState.colliding && verticalTranslation.y < 0)
                return verticalTranslation.normalized * totalStepHeight;
            else
                return verticalTranslation;

        }

        public void AddHorizontalSubCollider(CylinderCollider collider)
        {
            if (!horizontalSubColliders.Contains(collider))
                horizontalSubColliders.Add(collider);
        }

        public void RemoveHorizontalSubCollider(CylinderCollider collider)
        {
            if (horizontalSubColliders.Contains(collider))
                horizontalSubColliders.Remove(collider);
        }

        public void AddVerticalSubCollider(CylinderCollider collider)
        {
            if (!verticalSubColliders.Contains(collider))
                verticalSubColliders.Add(collider);
        }

        public void RemoveVerticalSubCollider(CylinderCollider collider)
        {
            if (verticalSubColliders.Contains(collider))
                verticalSubColliders.Remove(collider);
        }

        private int CheckVerticalCollision(float distance)
        {
            int count = collider.CheckVerticalCollision(distance);
            for (int i = 0; i < verticalSubColliders.Count; i++)
                count += verticalSubColliders[i].CheckVerticalCollision(distance);

            return count;
        }

        private RaycastHit GetClosestVerticalHits()
        {
            RaycastHit hit = collider.GetClosestVerticalHit();
            for (int i = 0; i < verticalSubColliders.Count; i++)
            {
                var newHit = verticalSubColliders[i].GetClosestVerticalHit();
                hit = newHit.distance < hit.distance ? newHit : hit;
            }

            return hit;
        }
        private int CheckHorizontalCollision(Vector3 velocity, Vector3 displacement)
        {
            int count = collider.CheckHorizontalCollision(velocity, displacement);
            for (int i = 0; i < horizontalSubColliders.Count; i++)
                count += horizontalSubColliders[i].CheckHorizontalCollision(velocity, displacement);

            return count;
        }

        private RaycastHit GetClosestHorizontalHits()
        {
            RaycastHit hit = collider.GetClosestHorizontalHit();
            for (int i = 0; i < horizontalSubColliders.Count; i++)
            {
                var newHit = horizontalSubColliders[i].GetClosestHorizontalHit();
                hit = newHit.distance < hit.distance ? newHit : hit;
            }

            return hit;
        }

        private void Translate(Vector3 translation)
        {
            transform.Translate(translation, Space.World);
        }

        private bool LessThan(float first, float second)
        {
            return second - first > MIN_DIFFERENCE;
        }

        private bool MoreThan(float first, float second)
        {
            return first - second > MIN_DIFFERENCE;
        }

        [System.Serializable]
        protected struct CollisionState
        {
            public bool colliding;
            public Vector3 normal;
            public SlopeState slopeState;

            public void Reset()
            {
                colliding = false;
                normal = Vector3.zero;
                slopeState = SlopeState.Flat;
            }

            public enum SlopeState { Flat, Slope, StepSlope }
        }
    }
}