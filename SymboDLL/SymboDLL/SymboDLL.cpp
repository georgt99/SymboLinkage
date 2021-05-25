#include "pch.h" // use stdafx.h in Visual Studio 2017 and earlier
#include <utility>
#include <limits.h>
#include "SymboDLL.h"

using namespace std;

#include "Linkage_Data.cpp"

// Eigen
#include <Eigen/Core>
#include <Eigen/Geometry>
using namespace Eigen;

// autodiff
#include <autodiff/forward.hpp>
#include <autodiff/forward/eigen.hpp>
using namespace autodiff;


// DLL internal state variables:
static vector<StaticVertex> static_verts;
static vector<MotorizedVertex> motorized_verts;
static vector<DynamicVertex> dynamic_verts; // these need to be ordered by dependencies
static int num_vertices;

// data preparation

void init() {
	static_verts = vector<StaticVertex>();
	motorized_verts = vector<MotorizedVertex>();
	dynamic_verts = vector<DynamicVertex>();
	num_vertices = 0;
}

int add_static_vertex(float x, float y) {
	StaticVertex new_vert = StaticVertex(x, y, num_vertices++);
	static_verts.push_back(new_vert);
	return new_vert.index;
}

int add_motorized_vertex(float x, float y, int motor_vertex, float distance_to_motor) {
	MotorizedVertex new_vert = MotorizedVertex(x, y, motor_vertex, distance_to_motor, num_vertices++);
	motorized_verts.push_back(new_vert);
	return new_vert.index;
}

int add_dynamic_vertex(float x, float y) {
	DynamicVertex new_vert = DynamicVertex(x, y, num_vertices++);
	dynamic_verts.push_back(new_vert);
}

bool prepare_simulation() {
	// BIG TODO
}


// control
void set_motor_rotation(int vertex_index, float rotation) {
	for (MotorizedVertex m_vert : motorized_verts) {
		if (m_vert.index == vertex_index) {
			m_vert.current_rotation = rotation;
			return;
		}
	}
}

// simulation
void get_simulated_positions(float** output_array) {
	
	// static
	for (StaticVertex s_vert : static_verts) {
		output_array[s_vert.index][0] = s_vert.initial_x;
		output_array[s_vert.index][1] = s_vert.initial_y;
	}

	// motorized
	for (MotorizedVertex m_vert : motorized_verts) {
		Vector2f motor_position(output_array[m_vert.motor_vertex][0], output_array[m_vert.motor_vertex][1]);
		
		Rotation2D rot(m_vert.current_rotation);
		Vector2f rotated_position = rot.toRotationMatrix() * Vector2f(m_vert.distance_to_motor, 0) + motor_position;

		output_array[m_vert.index][0] = rotated_position.x();
		output_array[m_vert.index][1] = rotated_position.y();
	}

	// dynamic
	for (DynamicVertex d_vert : dynamic_verts) {


		Vector2f i(output_array[d_vert.dependant_i][0], output_array[d_vert.dependant_i][1]);
		Vector2f j(output_array[d_vert.dependant_j][0], output_array[d_vert.dependant_j][1]);
		float dist_ik = d_vert.distance_to_i;
		float dist_jk = d_vert.distance_to_j;
		float dist_ij = (i - j).norm();
		float phi = acos(
			(dist_ij * dist_ij + dist_ik * dist_ik - dist_jk * dist_jk)
			/ (2 * dist_ij * dist_ik)
		);

		Rotation2D phi_rotation(phi);
		Vector2f k = phi_rotation.toRotationMatrix() * (dist_ik * (j - i) / (j - i).norm()) + i;
		output_array[d_vert.index][0] = k.x(); output_array[d_vert.index][1] = k.y();
	}
}


// DEPRECATED

// i, j, k according to Disney paper
// This assumes that i -> j -> k traverses the triangle counter-clockwise.
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
