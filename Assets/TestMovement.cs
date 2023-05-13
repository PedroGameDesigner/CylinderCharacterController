using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestMovement : MonoBehaviour
{
    [SerializeField] private float hSpeed = 4f;
    [SerializeField] private float vSpeed = 4f;
    [SerializeField] private float rotation = 20f;
    [SerializeField] private float jumpFrequency = 6;

    private CylinderCollider collider;
    Vector3 direction;
    Vector3 verticalDirection;
    RaycastHit[] hits;

    float timer;

    // Update is called once per frame
    private void Start()
    {
        direction = Vector3.forward;
        collider = GetComponent<CylinderCollider>();
    }

    void Update()
    {
        direction = Quaternion.AngleAxis(rotation * Time.deltaTime, Vector3.up) * direction;
        verticalDirection = Mathf.Sin(0.5f * Mathf.PI * (timer * jumpFrequency)) * Vector3.up;
        Vector3 translation = (direction * hSpeed + verticalDirection * vSpeed)* Time.deltaTime;
        Vector3 horTranslaction = GetHorizontalTranslation(translation);
        Vector3 verTranslation = GetVerticalTranslation(translation);

        transform.Translate(horTranslaction + verTranslation, Space.World);
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        timer += Time.deltaTime;
    }

    private Vector3 GetHorizontalTranslation(Vector3 totalTranslation)
    {
        Vector3 translation = Vector3.ProjectOnPlane(totalTranslation, Vector3.up);
        int hitCount = collider.CheckHorizontalCollision(translation);
        if (hitCount > 0)
        {
            float distance = translation.magnitude;
            for (int i = 0; i < hitCount; i++)
            {
                if (distance > collider.LastHorizontalHits[i].distance)
                    distance = collider.LastHorizontalHits[i].distance;
            }
            translation = distance * translation.normalized;
        }

        return translation;
    }

    private Vector3 GetVerticalTranslation(Vector3 totalTranslation)
    {
        int hitCount = collider.CheckVerticalCollision(totalTranslation.y);
        float sign = Mathf.Sign(totalTranslation.y);
        float translation = Mathf.Abs(totalTranslation.y);
        if (hitCount > 0)
        {
            for (int i = 0; i < hitCount; i++)
            {
                if (translation > collider.LastVerticalHits[i].distance)
                    translation = collider.LastVerticalHits[i].distance;
            }
        }

        return translation * Vector3.up * sign;
    }
}
