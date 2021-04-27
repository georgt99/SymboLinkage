using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Linkage : MonoBehaviour
{

    private List<Joint> orderedJoints;

    private void OnDrawGizmos()
    {
        foreach (Joint j in GetComponentsInChildren<Joint>())
        {
            foreach (Joint j2 in j.initialEdges)
            {
                if (j2) Gizmos.DrawLine(j.transform.position, j2.transform.position);
            }
            if (j.isAnchored) Gizmos.DrawWireSphere(j.transform.position, j.transform.lossyScale.x * 1.3f/2f);
        }
    }


    private void Start()
    {
        InitializeEdges();
        orderedJoints = GetJointOrderForSimulation();
    }

    private void InitializeEdges()
    {
        foreach (Joint j1 in GetComponentsInChildren<Joint>())
        {
            foreach (Joint j2 in j1.initialEdges) // initial edges are directed for simpler storage
            {
                Edge newEdge = new Edge
                {
                    j1 = j1,
                    j2 = j2,
                    length = Vector3.Distance(j1.transform.position, j2.transform.position)
                };
                j1.edges.Add(newEdge);
                j2.edges.Add(newEdge);
            }
        }
    }

    private void Update()
    {
        SimulateLinkages();
    }

    private void SimulateLinkages()
    {
        List<Joint> calculated = new List<Joint>();
        foreach (Joint current in orderedJoints)
        {
            List<Joint> fixedJoints = new List<Joint>();
            foreach (Edge edge in current.edges)
            {
                Joint adj = edge.GetOtherJoint(current);
                if (adj.isAnchored || calculated.Contains(adj))
                {
                    fixedJoints.Add(adj);
                    if (fixedJoints.Count >= 2) break;
                }
            }

            // i, j, k according to Disney paper
            Vector3 i = fixedJoints[0].transform.position;
            Vector3 j = fixedJoints[1].transform.position;

            float distIJ = Vector3.Distance(i, j);
            float distIK = fixedJoints[0].GetEdgeToJoint(current).length;
            float distJK = fixedJoints[1].GetEdgeToJoint(current).length;
            float phi = Mathf.Acos(
                (distIJ * distIJ + distIK * distIK - distJK * distJK)
                /(2 * distIJ * distIK));
            Debug.Log(phi);

            Plane triPlane = new Plane(i, j, current.transform.position);
            Vector3 triNormal = triPlane.normal;

            Quaternion R_phi = Quaternion.AngleAxis(Mathf.Rad2Deg * phi, triNormal);// todo: don't always use the 2D-normal
            
            Vector3 k = R_phi * (distIK * (j - i) / Vector3.Magnitude(j - i)) + i;
            current.transform.position = k;

            calculated.Add(current);
        }
    }

    private List<Joint> GetJointOrderForSimulation()
    {
        Dictionary<Joint, int> numberOfFixedAdjacents = new Dictionary<Joint, int>();
        foreach (Joint j in GetComponentsInChildren<Joint>())
        {
            if (!j.isAnchored)
            {
                numberOfFixedAdjacents[j] = 0;
            }
        }
        
        Queue<Joint> readyJoints = new Queue<Joint>();
        foreach (Joint j in GetComponentsInChildren<Joint>())
        {
            if (j.isAnchored)
            {
                foreach (Edge e in j.edges)
                {
                    Joint adj = e.GetOtherJoint(j);
                    if (adj.isAnchored) continue;
                    numberOfFixedAdjacents[adj]++;
                    if (numberOfFixedAdjacents[adj] == 2)
                    {
                        readyJoints.Enqueue(adj);
                    }
                }
            }
        }
        List<Joint> order = new List<Joint>();
        while (readyJoints.Count > 0)
        {
            Joint current = readyJoints.Dequeue();
            order.Add(current);
            foreach (Edge edge in current.edges)
            {
                Joint adj = edge.GetOtherJoint(current);
                //Debug.Log(current.gameObject.name + " has edge to " + adj.gameObject.name);
                if (adj.isAnchored) continue;
                numberOfFixedAdjacents[adj]++;
                if (numberOfFixedAdjacents[adj] == 2)
                {
                    readyJoints.Enqueue(adj);
                }
            }
        }
        return order;
    }


}



// Editor

[CustomEditor(typeof(Linkage))]
public class LinkageEditor : Editor
{
    private Joint previousSelection;

    public void OnSceneGUI()
    {
        Linkage links = (Linkage)target;

        if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
        {
            Ray worldRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            RaycastHit hitInfo;

            if (Physics.Raycast(worldRay, out hitInfo))
            {
                Joint selected;
                if (selected = hitInfo.collider.GetComponent<Joint>())
                {
                    if (previousSelection == null) // first selection
                    {
                        previousSelection = selected;
                    }
                    else // second selection
                    {
                        if (previousSelection == selected) // selected again, undo selection
                        {
                            previousSelection = null;
                        }
                        else if (previousSelection.initialEdges.Contains(selected))
                        {
                            Undo.RecordObject(previousSelection, "Remove Edge");
                            previousSelection.initialEdges.Remove(selected);
                        }
                        else if (selected.initialEdges.Contains(previousSelection))
                        {
                            Undo.RecordObject(selected, "Remove Edge");
                            selected.initialEdges.Remove(previousSelection);
                        }
                        else // didn't exist yet, add from previous to new
                        {
                            Undo.RecordObject(previousSelection, "Add Edge");
                            previousSelection.initialEdges.Add(selected);
                        }
                        previousSelection = null;
                    }
                    Event.current.Use();
                }
            }
        }
        if (previousSelection)
        {
            Handles.DrawWireCube(previousSelection.transform.position, previousSelection.transform.lossyScale);
        }
    }
}
