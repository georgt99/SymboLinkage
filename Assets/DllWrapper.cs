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
    [DllImport("SymboDLL")]
    private static extern void get_edge_length_gradients_for_target_position(
        int vertex_index, float x, float y,
        [In, Out] float[] first_end, [In, Out] float[] second_end, [In, Out] float[] edge_length_gradient);
    [DllImport("SymboDLL")]
    private static extern bool optimize_for_target_location(
        int vertex_index, float x, float y);
    [DllImport("SymboDLL")]
    private static extern bool optimize_for_target_path(
    int vertex_index, int number_of_path_points, [In, Out] float[] path_x, [In, Out] float[] path_y, int simulation_resolution);


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

    public static void GetEdgeLengthGradientsForTargetPosition(int vertexIndex, Vector2 targetPos,
        float[] firstEnd, float[] secondEnd, float[] edgeLengthGradient)
    {
        get_edge_length_gradients_for_target_position(vertexIndex, targetPos.x, targetPos.y,
            firstEnd, secondEnd, edgeLengthGradient);
    }

    public static bool OptimizeForTargetLocation(int vertex_index, Vector2 target)
    {
        return optimize_for_target_location(vertex_index, target.x, target.y);
    }

    public static bool OptimizeForTargetPath(int vertex_index, Vector2[] targetPath, int simulationResolution)
    {
        float[] path_x = new float[targetPath.Length];
        float[] path_y = new float[targetPath.Length];
        for (int i = 0; i < targetPath.Length; i++)
        {
            path_x[i] = targetPath[i].x;
            path_y[i] = targetPath[i].y;
        }
        return optimize_for_target_path(vertex_index, targetPath.Length, path_x, path_y, simulationResolution);
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
