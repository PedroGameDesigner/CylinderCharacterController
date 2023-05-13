using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.InputSystem.InputAction;

public class PlayerCharacter : MonoBehaviour
{
    public bool ascend;
    public Vector3 direction;

    private CharacterPhysics physics;

    private void Awake()
    {
        physics = GetComponent<CharacterPhysics>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 speed = Vector3.zero;
        if (ascend)
            speed += Vector3.up * 6f;
        else
            speed += Vector3.down * 6f;

        speed += direction * 4f;
        physics.Speed = speed;
        if (direction.magnitude >= 0.01f)
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
    }

    public void OnAscendChange(CallbackContext context)
    {
        if (context.performed)
            ascend = true;
        else if (context.canceled)
            ascend = false;
    }

    public void OnMoveChange(CallbackContext context)
    {
        Vector3 value = context.action.ReadValue<Vector2>();
        direction = new Vector3(value.x, 0, value.y);
    }
}
