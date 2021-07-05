using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Linkage3D : MonoBehaviour
{
    public float coneHeight = 1;
    public float coneRadius = 1;

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