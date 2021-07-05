using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Joint3D : MonoBehaviour
{

    public List<Joint3D> initialEdges; // technically directed because it's easier to store
    [HideInInspector]
    public List<Edge> edges = new List<Edge>(); // undirected, initialized by Linkage3D
    public int index;
    public bool isAnchored = false;


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
    }
}

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
