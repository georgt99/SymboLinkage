using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class dlltest : MonoBehaviour
{
    [DllImport("SymboDLL")]
    private static extern int add1(int x);
    [DllImport("SymboDLL")]
    private static extern float magnitude(float x, float y, float z);

    private int i = 0;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            i = add1(i);
            Debug.Log(i);
        }

        Vector3 pos = transform.position;
        Debug.Log(magnitude(pos.x, pos.y, pos.z));
    }
}
