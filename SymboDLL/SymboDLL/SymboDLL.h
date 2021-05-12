// MathLibrary.h - Contains declarations of math functions
#pragma once

#ifdef SYMBOLINKAGE_EXPORTS
#define SYMBOLINKAGE_API __declspec(dllexport)
#else
#define SYMBOLINKAGE_API __declspec(dllimport)
#endif


extern "C" SYMBOLINKAGE_API int add1(const int x);

//extern "C" SYMBOLINKAGE_API float magnitude(float x, float y, float z);
