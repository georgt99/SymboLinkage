#include "pch.h" // use stdafx.h in Visual Studio 2017 and earlier
#include <utility>
#include <limits.h>
#include "SymboDLL.h"

using namespace std;

// Eigen
#include <Eigen/Core>
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

