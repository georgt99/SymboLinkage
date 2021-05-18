using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Linkage : MonoBehaviour
{
    public GameObject linkPrefab;

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
        GameObject linkHolder = new GameObject("LinkHolder");
        linkHolder.transform.parent = transform;
        linkHolder.transform.localPosition = Vector3.zero;
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
                // add prefab for visual
                Link newLink = Instantiate(linkPrefab, linkHolder.transform).GetComponent<Link>();
                newLink.j1 = j1; newLink.j2 = j2;
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

            // ensure correct order of i and j
            int indexI = 0, indexJ = 1;
            Vector3 triNormal = new Plane(
                fixedJoints[indexI].transform.position,
                fixedJoints[indexJ].transform.position,
                current.transform.position).normal;
            if (triNormal.z < 0)
            {
                indexI = 1; indexJ = 0;
            }

            // i, j, k according to Disney paper
            Vector3 i = fixedJoints[indexI].transform.position;
            Vector3 j = fixedJoints[indexJ].transform.position;
            float distIK = fixedJoints[indexI].GetEdgeToJoint(current).length;
            float distJK = fixedJoints[indexJ].GetEdgeToJoint(current).length;

            Vector3 k = DllWrapper.SymbolicKinematic(i, j, distIK, distJK);
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
            Handles.DrawWireCube(previousSelection.transform.position, previousSelection.transform.lossyScale);
        }
    }
}
