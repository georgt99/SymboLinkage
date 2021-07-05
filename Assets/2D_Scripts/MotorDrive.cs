using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MotorDrive : MonoBehaviour
{
    public Joint originJoint;
    public Vector3 axis = Vector3.forward;
    public float currentRotation; // in degrees
    public float automaticRotationSpeed; // in rotations per second


    private void Start()
    {
        currentRotation = Vector2.SignedAngle(Vector2.right, transform.position - originJoint.transform.position);
    }

    private void Update()
    {
        currentRotation += automaticRotationSpeed * 360 * Time.deltaTime;
        currentRotation = currentRotation % 360;
    }



    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, transform.lossyScale);
    }
}
