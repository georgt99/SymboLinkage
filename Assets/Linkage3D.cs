using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Linkage3D : MonoBehaviour
{
    public float coneHeight = 1;
    public float coneRadius = 1;
    public Vector3 coneTip { get => transform.position + Vector3.up * coneHeight; }

    private List<Joint3D> orderedJoints;

    private void OnDrawGizmos()
    {
        int separations = 30;
        for (float angle = 0; angle < 360; angle += 360f / separations)
        {
            Gizmos.DrawLine(transform.position + Vector3.up * coneHeight,
                transform.position + Quaternion.Euler(0, angle, 0) * (Vector3.right * coneRadius));
            Gizmos.DrawLine(transform.position + Quaternion.Euler(0, angle, 0) * (Vector3.right * coneRadius),
                transform.position + Quaternion.Euler(0, angle + 360f / separations, 0) * (Vector3.right * coneRadius));
        }

    }

    private void Start()
    {
        InitializeEdges();
        orderedJoints = GetJointOrderForSimulation();
    }

    private void InitializeEdges()
    {
        foreach (Joint3D j1 in GetComponentsInChildren<Joint3D>())
        {
            j1.distToConeTip = Vector3.Distance(j1.transform.position, coneTip);
            foreach (Joint3D j2 in j1.initialEdges) // initial edges are directed for simpler storage
            {
                Edge3D newEdge = new Edge3D
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

    private List<Joint3D> GetJointOrderForSimulation()
    {
        Dictionary<Joint3D, List<Edge3D>> fixedAdjacents = new Dictionary<Joint3D, List<Edge3D>>();
        foreach (Joint3D j in GetComponentsInChildren<Joint3D>())
        {
            if (!j.isAnchored)
            {
                fixedAdjacents[j] = new List<Edge3D>();
            }
        }

        Queue<Joint3D> readyJoints = new Queue<Joint3D>();
        foreach (Joint3D j in GetComponentsInChildren<Joint3D>())
        {
            if (j.isAnchored)
            {
                foreach (Edge3D e in j.edges)
                {
                    Joint3D adj = e.GetOtherJoint(j);
                    if (adj.isAnchored) continue;
                    fixedAdjacents[adj].Add(e);
                    if (fixedAdjacents[adj].Count == 2)
                    {
                        readyJoints.Enqueue(adj);
                    }
                }
            }
        }
        List<Joint3D> order = new List<Joint3D>();
        while (readyJoints.Count > 0)
        {
            Joint3D current = readyJoints.Dequeue();
            order.Add(current);

            Edge3D dep1 = fixedAdjacents[current][0];
            Edge3D dep2 = fixedAdjacents[current][1];
            if (new Plane(
                current.transform.position,
                dep1.GetOtherJoint(current).transform.position,
                dep1.GetOtherJoint(current).transform.position).GetSide(coneTip) == false) // ensure correct orientation
            {
                Edge3D tmp = dep1;
                dep1 = dep2;
                dep2 = tmp;
            }

            current.dependant1 = dep1;
            current.dependant2 = dep2;
            
            foreach (Edge3D edge in current.edges)
            {
                Joint3D adj = edge.GetOtherJoint(current);
                if (adj.isAnchored) continue;
                fixedAdjacents[adj].Add(edge);
                if (fixedAdjacents[adj].Count == 2)
                {
                    readyJoints.Enqueue(adj);
                }
            }
        }
        return order;
    }

    private void Update()
    {
        foreach (Joint3D current in orderedJoints)
        {
            current.SimulatePosition();
        }
    }

}



// Editor

[CustomEditor(typeof(Linkage3D))]
public class Linkage3DEditor : Editor
{
    private Joint3D previousSelection;

    public void OnSceneGUI()
    {
        Linkage3D links = (Linkage3D)target;

        if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
        {
            Ray worldRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            RaycastHit hitInfo;

            if (Physics.Raycast(worldRay, out hitInfo))
            {
                Joint3D selected;
                if (selected = hitInfo.collider.GetComponentInParent<Joint3D>())
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
                            PrefabUtility.RecordPrefabInstancePropertyModifications(previousSelection);
                        }
                        else if (selected.initialEdges.Contains(previousSelection))
                        {
                            Undo.RecordObject(selected, "Remove Edge");
                            selected.initialEdges.Remove(previousSelection);
                            PrefabUtility.RecordPrefabInstancePropertyModifications(selected);
                        }
                        else // didn't exist yet, add from previous to new
                        {
                            Undo.RecordObject(previousSelection, "Add Edge");
                            previousSelection.initialEdges.Add(selected);
                            PrefabUtility.RecordPrefabInstancePropertyModifications(previousSelection);
                        }
                        previousSelection = null;
                    }
                    Event.current.Use();
                }
            }
        }
        if (previousSelection)
        {
            Handles.DrawWireCube(previousSelection.transform.position, previousSelection.transform.lossyScale / 4f);
        }
    }
}