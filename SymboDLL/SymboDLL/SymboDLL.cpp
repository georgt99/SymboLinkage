#include "pch.h" // use stdafx.h in Visual Studio 2017 and earlier
#include <utility>
#include <limits.h>
#include "SymboDLL.h"

using namespace std;

// Eigen
#include <Eigen/Core>
#include <Eigen/Geometry>
using namespace Eigen;

// autodiff
#include <autodiff/forward.hpp>
#include <autodiff/forward/eigen.hpp>
using namespace autodiff;


// DLL internal state variables:
static unsigned int myPlaceholderVariable;


// JUST FOR TESTING - DELETE ME ASAP
int add1(const int x)
{
    return x + 1;
}

// JUST FOR TESTING
float magnitude(float x, float y, float z) { //Vector3dual for me i think...?
    Vector3f vec(x, y, z);
    return vec.norm();
}
void negate_vec2(float* vec_array) {
	//Vector2f v(vec_array[0], vec_array[1]);
	//Vector2f negated = v.inverse();
	vec_array[0] = - vec_array[0]; vec_array[1] = - vec_array[1]; //vec_array[0] = negated.x(); vec_array[1] = negated.y();
	return; // TODO: is this needed?
}
void add_vec2s(float* vec1_array, float* vec2_array, float* result_array){
	Vector2f vec1(vec1_array[0], vec1_array[1]);
	Vector2f vec2(vec2_array[0], vec2_array[1]);
	Vector2f result = vec1 + vec2;
	result_array[0] = result.x(); result_array[1] = result.y();
}


void symbolic_kinematic(
	float* i_array, float* j_array,
	float dist_ik, float dist_jk,
	float* output_array
) {
	Vector2f i(i_array[0], i_array[1]);
	Vector2f j(j_array[0], j_array[1]);
	float dist_ij = (i - j).norm();
	float phi = acos(
		(dist_ij * dist_ij + dist_ik * dist_ik - dist_jk * dist_jk)
		/ (2 * dist_ij * dist_ik)
	);

	Rotation2D phi_rotation(phi);
	Vector2f k = phi_rotation.toRotationMatrix() * (dist_ik * (j - i) / (j - i).norm()) + i;
	output_array[0] = k.x(); output_array[1] = k.y();
}
