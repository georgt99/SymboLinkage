using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Joint : MonoBehaviour
{
    public List<Joint> initialEdges;
    [HideInInspector]
    public List<Edge> edges = new List<Edge>(); // initialized by Linkage
    public int index;
    public bool isAnchored = false;


    public Edge GetEdgeToJoint(Joint other)
    {
        foreach (Edge e in edges)
        {
            if (e.GetOtherJoint(this) == other) return e;
        }
        Debug.LogError("GetEdgeToJoint was called but edge didn't exist");
        return edges[0];
    }

    public bool HasEdgeToJoint(Joint other)
    {
        foreach(Edge e in edges)
        {
            if (e.GetOtherJoint(this) == other) return true;
        }
        return false;
    }
}


public class Edge
{
    public Joint j1;
    public Joint j2;
    public float length;

    public Joint GetOtherJoint(Joint thisJoint)
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
