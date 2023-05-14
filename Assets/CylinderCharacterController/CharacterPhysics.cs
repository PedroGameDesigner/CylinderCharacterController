using UnityEngine;

namespace CylinderCharacterController
{
    public class CharacterPhysics : MonoBehaviour
    {
        [SerializeField]
        bool debug = false;
        [SerializeField]
        private float maxSlopeAngle = 75;
        [SerializeField]
        private float totalStepHeight = 0.2f;
        [SerializeField]
        private float stepCount = 4;
        [SerializeField]
        private float minDifference = 0.001f;

        private new CylinderCollider collider;
        private CollisionState ceillingState = new CollisionState();
        private CollisionState floorState = new CollisionState();

        public Vector3 Speed { get; set; }

        private void Awake()
        {
            collider = GetComponent<CylinderCollider>();
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            float time = Time.fixedDeltaTime;
            Vector3 translation = Speed * time;
            Vector3 extraTranslation = ProcessVerticalDisplacement(translation.GetVerticalComponent());
            ProcessHorizontalDisplacement(translation.GetHorizontalComponent(), extraTranslation);
        }

        Vector3 ProcessVerticalDisplacement(Vector3 verticalTranslation)
        {
            Vector3 translation = verticalTranslation;
            Vector3 extraTranslation = Vector3.zero;
            int hitCount = collider.CheckVerticalCollision(translation.y);
            ceillingState.Reset();
            floorState.Reset();

            if (hitCount > 0)
            {
                RaycastHit closestHit = collider.GetClosestVerticalHit();
                if (translation.y > 0)
                {
                    UpdateCeilingState(closestHit);
                    if (ceillingState.slopeState == CollisionState.SlopeState.Flat || ceillingState.slopeState == CollisionState.SlopeState.Slope)
                        translation = CapTranslationToCollision(translation, closestHit);
                    else if (ceillingState.slopeState == CollisionState.SlopeState.StepSlope)
                    {
                        Vector3 oldTranslation = translation;
                        translation = CapTranslationToCollision(translation, closestHit);
                        extraTranslation = GetSteepSlopeTranslation(oldTranslation - translation, ceillingState);
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
            ceillingState.colliding = true;
            ceillingState.normal = hit.normal;
            float angle = Vector3.Angle(hit.normal, Vector3.down);

            if (angle > maxSlopeAngle) ceillingState.slopeState = CollisionState.SlopeState.StepSlope;
            else if (angle > 0) ceillingState.slopeState = CollisionState.SlopeState.Slope;
            else ceillingState.slopeState = CollisionState.SlopeState.Flat;
            Debug.Log($"Ceiling hit: normal={ceillingState.normal}, angle={angle}, slope={ceillingState.slopeState}");
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

        public Vector3 TryClimbSteps(Vector3 translation)
        {
            float distance = translation.magnitude;
            float subStep = totalStepHeight / stepCount;

            Vector3 bestHeigth = Vector3.zero;
            float bestDistance = 0;

            for (int i = 0; i < stepCount; i++)
            {
                Vector3 stepHeight = subStep * i * Vector3.up;
                int hitCount = collider.CheckHorizontalCollision(translation, stepHeight);
                if (hitCount <= 0)
                {
                    bestHeigth = stepHeight;
                    bestDistance = distance;
                    break;
                }
                else
                {
                    var hit = collider.GetClosestHorizontalHit();
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
                state = ceillingState;

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
            if (!floorState.colliding)
                return verticalTranslation;
            else
                return verticalTranslation.normalized * totalStepHeight;
        }

        private void Translate(Vector3 translation)
        {
            transform.Translate(translation, Space.World);
        }

        private bool LessThan(float first, float second)
        {
            return second - first > minDifference;
        }

        private bool MoreThan(float first, float second)
        {
            return first - second > minDifference;
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