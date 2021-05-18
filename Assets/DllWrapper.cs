using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class DllWrapper : MonoBehaviour
{
    [DllImport("SymboDLL")]
    private static extern void symbolic_kinematic(
        [In, Out] float[] i_array, [In, Out] float[] j_array,
        float dist_ik, float dist_jk,
        [In, Out] float[] result_array);


    /// <summary>
    /// Calculates k, given two points and their link length towards k.
    /// <br></br>
    /// <b>Important:</b> Ensure that i -> j -> k traverses the triangle counter-clockwise.
    /// </summary>
    public static Vector2 SymbolicKinematic(Vector2 i, Vector2 j, float distIK, float distJK)
    {
        float[] result = new float[2];
        symbolic_kinematic(Vec2ToArray(i), Vec2ToArray(j), distIK, distJK, result);
        return ArrayToVec2(result);
    }




    private static Vector2 ArrayToVec2(float[] arr)
    {
        return new Vector2(arr[0], arr[1]);
    }

    private static float[] Vec2ToArray(Vector2 vec)
    {
        return new float[] { vec.x, vec.y };
    }
}
