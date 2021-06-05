using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class DllWrapper : MonoBehaviour
{
    // deprecated
    [DllImport("SymboDLL")]
    private static extern void symbolic_kinematic(
        [In, Out] float[] i_array, [In, Out] float[] j_array,
        float dist_ik, float dist_jk,
        [In, Out] float[] result_array);


    [DllImport("SymboDLL")]
    private static extern void init();
    [DllImport("SymboDLL")]
    private static extern int add_static_vertex(float x, float y);
    [DllImport("SymboDLL")]
    private static extern int add_motorized_vertex(float x, float y, int motor_vertex);
    [DllImport("SymboDLL")]
    private static extern int add_dynamic_vertex(float x, float y);
    [DllImport("SymboDLL")]
    private static extern void add_edge(int index_1, int index_2);
    [DllImport("SymboDLL")]
    private static extern bool prepare_simulation();
    [DllImport("SymboDLL")]
    private static extern void set_motor_rotation(int vertex_index, float rotation);
    [DllImport("SymboDLL")]
    private static extern void get_simulated_positions(
        [In, Out] float[] x_output_array,
        [In, Out] float[] y_output_array);


    /// <summary>
    /// DEPRECATED | Calculates k, given two points and their link length towards k.
    /// <br></br>
    /// <b>Important:</b> Ensure that i -> j -> k traverses the triangle counter-clockwise.
    /// </summary>
    public static Vector2 SymbolicKinematic(Vector2 i, Vector2 j, float distIK, float distJK)
    {
        float[] result = new float[2];
        symbolic_kinematic(Vec2ToArray(i), Vec2ToArray(j), distIK, distJK, result);
        return ArrayToVec2(result);
    }



    public static void Init()
    {
        init();
    }

    public static int AddStaticVertex(Vector2 position)
    {
        return add_static_vertex(position.x, position.y);
    }

    public static int AddMotorizedVertex(Vector2 position, int motorVertex)
    {
        return add_motorized_vertex(position.x, position.y, motorVertex);
    }

    public static int AddDynamicVertex(Vector2 position)
    {
        return add_dynamic_vertex(position.x, position.y);
    }

    public static void addEdge(int index1, int index2)
    {
        add_edge(index1, index2);
    }

    public static bool PrepareSimulation()
    {
        return prepare_simulation();
    }

    public static void setMotorRotation(int vertexIndex, float rotation)
    {
        set_motor_rotation(vertexIndex, rotation);
    }

    public static void getSimulatedPositions(float[] x_output_array, float[] y_output_array)
    {
        get_simulated_positions(x_output_array, y_output_array);
    }


    // helpers

    private static Vector2 ArrayToVec2(float[] arr)
    {
        return new Vector2(arr[0], arr[1]);
    }

    private static float[] Vec2ToArray(Vector2 vec)
    {
        return new float[] { vec.x, vec.y };
    }
}
