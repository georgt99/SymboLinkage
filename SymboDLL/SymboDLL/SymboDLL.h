// MathLibrary.h - Contains declarations of math functions
#pragma once

#ifdef SYMBOLINKAGE_EXPORTS
#define SYMBOLINKAGE_API __declspec(dllexport)
#else
#define SYMBOLINKAGE_API __declspec(dllimport)
#endif

// data preparation
extern "C" SYMBOLINKAGE_API void init();
extern "C" SYMBOLINKAGE_API int add_static_vertex(float x, float y);
extern "C" SYMBOLINKAGE_API int add_motorized_vertex(float x, float y, int motor_vertex, float distance_to_motor);
extern "C" SYMBOLINKAGE_API int add_dynamic_vertex(float x, float y);
extern "C" SYMBOLINKAGE_API void add_edge(int index_1, int index_2);
extern "C" SYMBOLINKAGE_API bool prepare_simulation();

// control
extern "C" SYMBOLINKAGE_API void set_motor_rotation(int vertex_index, float rotation); // TODO: this can fail

// simulation
extern "C" SYMBOLINKAGE_API void get_simulated_positions(float* x_output_array, float* y_output_array);




// DEPRECATED
extern "C" SYMBOLINKAGE_API void symbolic_kinematic(
	float* i_array, float* j_array,
	float dist_ik, float dist_jk,
	float* output_array
);
