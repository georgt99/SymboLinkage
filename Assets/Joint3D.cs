using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Joint3D : MonoBehaviour
{

    public List<Joint3D> initialEdges; // technically directed because it's easier to store
    [HideInInspector]
    public List<Edge3D> edges = new List<Edge3D>(); // undirected, initialized by Linkage3D
    public int index;
    public bool isAnchored = false;
    public Edge3D dependant1;
    public Edge3D dependant2;
    public float distToConeTip;
    private Linkage3D linkage;

    private void Start()
    {
        linkage = GetComponentInParent<Linkage3D>();
    }


    public void SimulatePosition()
    {
        transform.position = Get4thPointOfTetrahedron(
            dependant1.GetOtherJoint(this).transform.position,
            dependant2.GetOtherJoint(this).transform.position,
            linkage.coneTip,
            dependant1.length, dependant2.length, distToConeTip);
    }

    private Vector3 Get4thPointOfTetrahedron(Vector3 p1, Vector3 p2, Vector3 p3, float r1, float r2, float r3)
    {
        // https://math.stackexchange.com/questions/3753340/finding-the-coordinates-of-the-fourth-vertex-of-tetrahedron-given-coordinates-o

        Vector3 uAxis = (p2 - p1).normalized;
        Vector3 vAxis = ((p3 - p1) - Vector3.Dot((p3 - p1), uAxis) * uAxis).normalized;
        Vector3 wAxis = Vector3.Cross(uAxis, vAxis);

        float u2 = Vector3.Dot(p2 - p1, uAxis);
        float u3 = Vector3.Dot(p3 - p1, uAxis);
        float v3 = Vector3.Dot(p3 - p1, vAxis);

        float u = (r1 * r1 - r2 * r2 + u2 * u2) / (2f * u2);
        float v = (r1 * r1 - r3 * r3 + u3 * u3 + v3 * v3 - 2f * u * u3) / (2f * v3);
        float w = Mathf.Sqrt(Mathf.Abs(r1 * r1 - u * u - v * v));

        return p1 + u * uAxis + v * vAxis + w * wAxis;
    }


    private void OnDrawGizmos()
    {
        // snapping to cone
        if (!Application.isPlaying)
        {
            if (transform.parent == null) return;
            Linkage3D linkage;
            if (linkage = transform.parent.GetComponent<Linkage3D>())
            {
                float angleHere = Vector3.SignedAngle(
                    Vector3.right, new Vector3(transform.localPosition.x, 0,
                    transform.localPosition.z), Vector3.up);
                float radiusHere = Mathf.Lerp(
                    linkage.coneRadius, 0, Mathf.InverseLerp(0, linkage.coneHeight, transform.localPosition.y));
                Vector3 snappedPosition = Quaternion.Euler(0, angleHere, 0) * (Vector3.right * radiusHere)
                    + Vector3.up * transform.localPosition.y;
                transform.localPosition = snappedPosition;

                transform.LookAt(linkage.transform.position + Vector3.up * linkage.coneHeight);
            }
        }
        // display edges
        foreach (Joint3D j in initialEdges)
            Gizmos.DrawLine(transform.position, j.transform.position);

        if (isAnchored)
        {
            //Gizmos.DrawWireSphere(transform.position, transform.lossyScale.x / 4f);
        }
    }
}

[System.Serializable]
public class Edge3D
{
    public Joint3D j1;
    public Joint3D j2;
    public float length;

    public Joint3D GetOtherJoint(Joint3D thisJoint)
    {
        if (thisJoint == j1)
            return j2;
        else if (thisJoint == j2)
            return j1;
        else
        {
            Debug.LogError("GetOtherJoint recieved an unconnected joint");
            return j1;
        }
    }

}
