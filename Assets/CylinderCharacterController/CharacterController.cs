using UnityEngine;

namespace CylinderCharacterController
{
    public abstract class CharacterController : MonoBehaviour
    {
        protected CharacterPhysics physics;

        public abstract Vector3 Speed { get; }

        protected void Awake()
        {
            physics = GetComponent<CharacterPhysics>();
        }

        protected virtual void FixedUpdate()
        {
            physics.Speed = Speed;
        }
    }
}