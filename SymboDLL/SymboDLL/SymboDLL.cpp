#include "pch.h" // use stdafx.h in Visual Studio 2017 and earlier
#include <utility>
#include <limits.h>
#include "SymboDLL.h"

using namespace std;
/*
// Eigen
#include "Eigen/Core"
using namespace Eigen;

// autodiff
#include "autodiff/forward.hpp"
#include "autodiff/forward/eigen.hpp"
//using namespace autodiff;
*/

// DLL internal state variables:
static unsigned int myPlaceholderVariable;


// JUST FOR TESTING - DELETE ME ASAP
int add1(const int x)
{
    return x + 1;
}
/*
// JUST FOR TESTING
float magnitude(float x, float y, float z) { //Vector3dual for me i think...?
    Vector3f vec(x, y, z);
    return vec.norm();
}*/

