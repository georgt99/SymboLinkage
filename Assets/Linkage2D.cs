using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Linkage2D : MonoBehaviour
{
    public GameObject linkPrefab;

    private Joint[] joints;

    

    private void OnDrawGizmos()
    {
        foreach (Joint j in GetComponentsInChildren<Joint>())
        {
            foreach (Joint j2 in j.initialEdges)
            {
                if (j2) Gizmos.DrawLine(j.transform.position, j2.transform.position);
            }
            if (j.isAnchored) Gizmos.DrawWireSphere(j.transform.position, j.transform.lossyScale.x * 1.3f / 2f);
        }
    }


    private void Start()
    {
        InitializeEdges();
        TransferLinkageToDLL();
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

    private void TransferLinkageToDLL()
    {
        DllWrapper.Init();
        // vertices
        joints = GetComponentsInChildren<Joint>();
        for (int i = 0; i < joints.Length; i++)
        {
            joints[i].index = i;
        }
        foreach (Joint j in joints)
        {
            MotorDrive motor;
            if (motor = j.GetComponent<MotorDrive>()) // motorized
            {
                DllWrapper.AddMotorizedVertex(
                    j.transform.position,
                    motor.originJoint.index,
                    (j.transform.position - motor.originJoint.transform.position).magnitude
                );
            }
            else if (j.isAnchored) // static
            {
                DllWrapper.AddStaticVertex(j.transform.position);
            }
            else // dymanic
            {
                DllWrapper.AddDynamicVertex(j.transform.position);
            }
        }

        // edges
        foreach (Joint j1 in joints)
        {
            foreach (Joint j2 in j1.initialEdges)
            {
                DllWrapper.addEdge(j1.index, j2.index);
            }
        }
        return;
        if (!DllWrapper.PrepareSimulation())
        {
            Debug.LogError("DLL-ERROR: Simulation coult not be prepared");
        }
    }

    private void Update()
    {
        UpdateMotors();
        //UpdateJointPositions();
    }

    private void UpdateMotors()
    {
        foreach (MotorDrive motor in GetComponentsInChildren<MotorDrive>())
        {
            DllWrapper.setMotorRotation(motor.GetComponent<Joint>().index, motor.currentRotation);
        }
    }

    private void UpdateJointPositions()
    {
        float[][] vertexCoordinates = new float[joints.Length][];
        for (int i = 0; i < joints.Length; i++)
        {
            vertexCoordinates[i] = new float[2];
        }
        DllWrapper.getSimulatedPositions(vertexCoordinates);
        for (int i = 0; i < joints.Length; i++)
        {
            joints[i].transform.position = new Vector2(vertexCoordinates[i][0], vertexCoordinates[i][1]);
        }

    }

}



// Editor

[CustomEditor(typeof(Linkage2D))]
public class Linkage2DEditor : Editor
{
    private Joint previousSelection;

    public void OnSceneGUI()
    {
        Linkage2D links = (Linkage2D)target;

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
