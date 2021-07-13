using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class PathOptimizer : MonoBehaviour
{
    public Joint jointToBeOptimized;
    public KeyCode keyToOptimize = KeyCode.P;
    public int simulationResolution = 10;

    private LineRenderer lr;

    private void Start()
    {
        lr = GetComponent<LineRenderer>();
    }

    private void Update()
    {
        if (Input.GetKey(keyToOptimize)){
            OptimizeForPath();
        }
    }

    private void OptimizeForPath()
    {
        Vector2[] path = new Vector2[lr.positionCount];
        for (int i = 0; i < lr.positionCount; i++)
        {
            if (lr.useWorldSpace)
            {
                path[i] = lr.GetPosition(i);
            } else
            {
                path[i] = lr.localToWorldMatrix * lr.GetPosition(i);
            }   
        }
        DllWrapper.OptimizeForTargetPath(jointToBeOptimized.index, path, simulationResolution);

    }

}
