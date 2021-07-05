using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MotorDrive3D : MonoBehaviour
{
    public Joint3D originJoint;
    public float currentRotation;

    private float lastRotation;

    private void Start()
    {
        currentRotation = Vector3.SignedAngle(originJoint.transform.right, transform.position - originJoint.transform.position, originJoint.transform.forward);
        lastRotation = currentRotation;
    }

    // TODO: move to C++
    private void Update()
    {
        if (lastRotation != currentRotation)
        {
            transform.RotateAround(originJoint.transform.position, originJoint.transform.forward, currentRotation - lastRotation);
            lastRotation = currentRotation;
        }

    }
}
