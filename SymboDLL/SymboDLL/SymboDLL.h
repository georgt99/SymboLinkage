// MathLibrary.h - Contains declarations of math functions
#pragma once

#ifdef SYMBOLINKAGE_EXPORTS
#define SYMBOLINKAGE_API __declspec(dllexport)
#else
#define SYMBOLINKAGE_API __declspec(dllimport)
#endif


extern "C" SYMBOLINKAGE_API void symbolic_kinematic(
	float* i_array, float* j_array,
	float dist_ik, float dist_jk,
	float* output_array
);
