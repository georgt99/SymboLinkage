// MathLibrary.h - Contains declarations of math functions
#pragma once

#ifdef SYMBOLINKAGE_EXPORTS
#define SYMBOLINKAGE_API __declspec(dllexport)
#else
#define SYMBOLINKAGE_API __declspec(dllimport)
#endif

namespace Symbo {

	// --- data preparation ---

	// must be called on startup, also acts as a reset
	extern "C" SYMBOLINKAGE_API void init();
	// add specified vertex and return its index, which is always equal to the number of add_?_vertex-calls so far
	extern "C" SYMBOLINKAGE_API int add_static_vertex(float x, float y);
	extern "C" SYMBOLINKAGE_API int add_motorized_vertex(float x, float y, int motor_vertex);
	extern "C" SYMBOLINKAGE_API int add_dynamic_vertex(float x, float y);
	// add link between vertices (order is irrelevant)
	extern "C" SYMBOLINKAGE_API void add_edge(int index_1, int index_2);
	// must be called after all vertices/edges have been added and before simulating
	extern "C" SYMBOLINKAGE_API bool prepare_simulation();

	// --- control ---
	extern "C" SYMBOLINKAGE_API void set_motor_rotation(int vertex_index, float rotation);

	// --- simulation ---
	extern "C" SYMBOLINKAGE_API void get_simulated_positions(float* x_output_array, float* y_output_array);

	extern "C" SYMBOLINKAGE_API void get_edge_length_gradients_for_target_position( // this should probably be split into multiple calls
		int vertex_index, float x, float y,
		float* first_end, float* second_end, float* edge_length_gradient
	);

	extern "C" SYMBOLINKAGE_API bool optimize_for_target_location(
		int vertex_index, float x, float y
	);

	extern "C" SYMBOLINKAGE_API bool optimize_for_target_path(
		int vertex_index, int number_of_path_points, float* path_x, float* path_y, int simulation_resolution
	);


	// DEPRECATED
	extern "C" SYMBOLINKAGE_API void symbolic_kinematic(
		float* i_array, float* j_array,
		float dist_ik, float dist_jk,
		float* output_array
	);

}
