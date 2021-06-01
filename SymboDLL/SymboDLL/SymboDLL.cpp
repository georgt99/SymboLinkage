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
static vector<DynamicVertex> dynamic_verts;
static vector<int> ordered_dymanic_indices; // ordered by dependence
static vector<Vertex> all_verts;
static int num_vertices;

// data preparation

void init() {
	static_verts = vector<StaticVertex>();
	motorized_verts = vector<MotorizedVertex>();
	dynamic_verts = vector<DynamicVertex>();
	ordered_dymanic_indices = vector<int>();
	all_verts = vector<Vertex>();
	num_vertices = 0;
}

int add_static_vertex(float x, float y) {
	StaticVertex new_vert = StaticVertex(x, y, num_vertices++);
	static_verts.push_back(new_vert);
	all_verts.push_back(new_vert);
	return new_vert.index;
}

int add_motorized_vertex(float x, float y, int motor_vertex, float distance_to_motor) {
	MotorizedVertex new_vert = MotorizedVertex(x, y, motor_vertex, distance_to_motor, num_vertices++);
	motorized_verts.push_back(new_vert);
	all_verts.push_back(new_vert);
	return new_vert.index;
}

int add_dynamic_vertex(float x, float y) {
	DynamicVertex new_vert = DynamicVertex(x, y, num_vertices++);
	dynamic_verts.push_back(new_vert);
	all_verts.push_back(new_vert);
	return new_vert.index;
}

void add_edge(int index_1, int index_2) {
	all_verts[index_1].edges.push_back(index_2);
	all_verts[index_2].edges.push_back(index_1);

	// UGLY QUICKFIX
	for (int i = 0; i < static_verts.size(); i++) {
		if (static_verts[i].index == index_1) static_verts[i].edges.push_back(index_2);
		if (static_verts[i].index == index_2) static_verts[i].edges.push_back(index_1);
	}
	for (int i = 0; i < motorized_verts.size(); i++) {
		if (motorized_verts[i].index == index_1) motorized_verts[i].edges.push_back(index_2);
		if (motorized_verts[i].index == index_2) motorized_verts[i].edges.push_back(index_1);
	}	for (int i = 0; i < dynamic_verts.size(); i++) {
		if (dynamic_verts[i].index == index_1) dynamic_verts[i].edges.push_back(index_2);
		if (dynamic_verts[i].index == index_2) dynamic_verts[i].edges.push_back(index_1);
	}
}

bool prepare_simulation() {
	// order dynamic vertices by dependence
	int sorted = 0;
	vector<vector<int>> dependencies = vector<vector<int>>(num_vertices, vector<int>());
	list<int> ready = list<int>();
	for (StaticVertex s_vert : static_verts) {
		for (int adj : s_vert.edges) {
			if (all_verts[adj].type == DYNAMIC) {
				dependencies[adj].push_back(s_vert.index);
				if (dependencies[adj].size() == 2) {
					ready.push_back(adj);
				}
			}
		}
	}
	for (MotorizedVertex m_vert : motorized_verts) {
		for (int adj : m_vert.edges) {
			if (all_verts[adj].type == DYNAMIC) {
				dependencies[adj].push_back(m_vert.index);
				if (dependencies[adj].size() == 2) {
					ready.push_back(adj);
				}
			}
		}
	}
	while (!ready.empty()) {
		int current = ready.front(); ready.pop_front();
		ordered_dymanic_indices.push_back(current);
		//set dependants
		Vector2f v0(all_verts[dependencies[current][0]].initial_x, all_verts[dependencies[current][0]].initial_y);
		Vector2f v1(all_verts[dependencies[current][1]].initial_x, all_verts[dependencies[current][1]].initial_y);

		for (DynamicVertex dyn_vert : dynamic_verts) {
			if (dyn_vert.index == current) {
				dyn_vert.dependant_i = dependencies[current][0];
				dyn_vert.dependant_j = dependencies[current][1];
				dyn_vert.distance_to_i = (Vector2f(dyn_vert.initial_x, dyn_vert.initial_y)
					- Vector2f(all_verts[dyn_vert.dependant_i].initial_x, all_verts[dyn_vert.dependant_i].initial_y)).norm();
				dyn_vert.distance_to_j = (Vector2f(dyn_vert.initial_x, dyn_vert.initial_y)
					- Vector2f(all_verts[dyn_vert.dependant_j].initial_x, all_verts[dyn_vert.dependant_j].initial_y)).norm();
				// TODO: fix orientation (counterclockwise)
				// use normal probably?
			}
		}

		sorted++;
		for (int adj : all_verts[current].edges) {
			if (all_verts[adj].type == DYNAMIC) {
				dependencies[adj].push_back(all_verts[current].index);
				if (dependencies[adj].size() == 2) {
					ready.push_back(adj);
				}
			}
		}
	}
	if (sorted < dynamic_verts.size()) {
		return false; // did not manage to fit all
	}
	return true;
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
