using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MotorDrive : MonoBehaviour
{
    public Joint targetJoint;
    public Vector3 axis = Vector3.forward;
    public float currentRotation;
    public float automaticRotationSpeed; // in rotations per second
    private float lastRotation;
    private float initialRotation;


    private void Update()
    {
        currentRotation += automaticRotationSpeed * 360 * Time.deltaTime;
        RotateTargetAroundMotor(lastRotation - currentRotation);
        lastRotation = currentRotation;
        currentRotation = currentRotation % 360;
    }

    private void RotateTargetAroundMotor(float angle)
    {
        targetJoint.transform.RotateAround(transform.position, axis, angle);

    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, transform.lossyScale);
    }
}
