using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TESTING_SymbolicKinematics3D : MonoBehaviour
{
    public Joint3D dependant1;
    public Joint3D dependant2;

    private float distTo1;
    private float distTo2;

    private void Start()
    {
        distTo1 = (transform.position - dependant1.transform.position).magnitude;
        distTo2 = (transform.position - dependant2.transform.position).magnitude;
    }

    private void Update()
    {
        Plane rotPlane1 = new Plane(dependant1.transform.position, dependant1.transform.forward);
        Plane rotPlane2 = new Plane(dependant2.transform.position, dependant2.transform.forward);

        // https://forum.unity.com/threads/how-to-find-line-of-intersecting-planes.109458/
        Vector3 intersectVec = Vector3.Cross(rotPlane1.normal, rotPlane2.normal).normalized;
        Vector3 ldir = Vector3.Cross(rotPlane2.normal, intersectVec);
        float numerator = Vector3.Dot(rotPlane1.normal, ldir);
        Vector3 intersectPoint = Vector3.zero;
        //Prevent divide by zero.
        if (Mathf.Abs(numerator) > 0.000001f)
        {
            Vector3 plane1ToPlane2 = dependant1.transform.position - dependant2.transform.position;
            float t = Vector3.Dot(rotPlane1.normal, plane1ToPlane2) / numerator;
            intersectPoint = dependant2.transform.position + t * ldir;
        }

        // https://stackoverflow.com/questions/51905268/how-to-find-closest-point-on-line
        Vector2 lhs = dependant1.transform.position - intersectPoint;
        float dotP = Vector2.Dot(lhs, intersectVec);
        Vector3 centerPoint = intersectPoint + intersectVec * dotP;

        float distCenterToDep1 = (centerPoint - dependant1.transform.position).magnitude;
        float distCenterToTarget = Mathf.Sqrt(distCenterToDep1 * distCenterToDep1 + distTo1 * distTo1);

        Vector3 target = centerPoint + intersectVec * distCenterToTarget;
        transform.position = target;
        Debug.DrawLine(centerPoint, dependant1.transform.position);
        Debug.DrawLine(centerPoint, dependant2.transform.position, Color.red);
        Debug.DrawLine(centerPoint, target);
    }
}
