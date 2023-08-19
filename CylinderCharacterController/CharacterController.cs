using UnityEngine;

namespace CylinderCharacterController
{
    public abstract class CharacterController : MonoBehaviour
    {
        protected CharacterPhysics physics;

        public abstract Vector3 Velocity { get; set; }

        protected virtual void Awake()
        {
            physics = GetComponent<CharacterPhysics>();
        }

        protected virtual void FixedUpdate()
        {
            physics.Speed = Velocity;
        }
        public void AddSubCollider(CylinderCollider collider)
        {
            physics.AddHorizontalSubCollider(collider);
        }

        public void RemoveSubCollider(CylinderCollider collider)
        {
            physics.RemoveHorizontalSubCollider(collider);
        }
    }
}