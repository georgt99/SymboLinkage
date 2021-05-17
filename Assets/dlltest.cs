using System;
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
    [DllImport("SymboDLL")]
    public static extern void negate_vec2( [In, Out] float[] vec_array);
    [DllImport("SymboDLL")]
    public static extern void add_vec2s(
        [In, Out] float[] vec1_array,
        [In, Out] float[] vec2_array,
        [In, Out] float[] result_array);

    private int i = 0;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            i = add1(i);
            Debug.Log(i);
        }

        Vector3 pos = transform.position;
        //Debug.Log(magnitude(pos.x, pos.y, pos.z));

        Vector2 pos2d = new Vector2(transform.position.x, transform.position.y);

        /*float[] negated = new float[] { pos2d.x, pos2d.y };
        negate_vec2(negated);
        Vector2 pos2d_negated = new Vector2(negated[0], negated[1]);
        Debug.Log(pos2d + " negated is " + pos2d_negated);*/

        float[] pos_array = new float[] { pos2d.x, pos2d.y };
        float[] result_array = new float[2];
        add_vec2s(pos_array, pos_array, result_array);
        Vector2 result = new Vector2(result_array[0], result_array[1]);
        Debug.Log(pos2d + " added to itself is " + result);
    }
}
