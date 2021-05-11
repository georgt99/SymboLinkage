#include "pch.h" // use stdafx.h in Visual Studio 2017 and earlier
#include <utility>
#include <limits.h>
#include "SymboDLL.h"

// DLL internal state variables:
static unsigned int myVariable;


int add1(const int x)
{
    return x + 1;
}
