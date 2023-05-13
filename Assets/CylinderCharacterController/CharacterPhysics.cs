using UnityEngine;

namespace CylinderCharacterController
{
    public class CharacterPhysics : MonoBehaviour
    {
        [SerializeField]
        private float maxSlopeAngle;

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
                    Debug.Log($"Ceiling hit: dist={closestHit.distance}, slope={ceillingState.slopeState}, t={extraTranslation}");
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
            int hitCount = collider.CheckHorizontalCollision(translation);
            if (hitCount > 0)
            {
                float distance = translation.magnitude;
                for (int i = 0; i < hitCount; i++)
                {
                    if (distance > collider.LastHorizontalHits[i].distance)
                    {
                        distance = collider.LastHorizontalHits[i].distance;
                        Debug.Log($"Hor Hit with: {collider.LastHorizontalHits[i].collider.name}");
                    }
                }
                translation = distance * translation.normalized;
            }

            Translate(translation.GetHorizontalComponent());
            ProcessVerticalDisplacement(translation.GetVerticalComponent());
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
                    {
                        //Debug.Break();
                        return extraTranslation;
                    }
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

            Debug.Log($"Ceiling hit: VerTrans={verticalTranslation}, state.normal={state.normal}, ");

            return rotation * state.normal * -Mathf.Abs(verticalTranslation.y);
        }

        private void Translate(Vector3 translation)
        {
            transform.Translate(translation, Space.World);
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