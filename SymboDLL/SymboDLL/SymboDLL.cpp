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

namespace Symbo {

	// DLL internal state variables:

	// Contains pointers to all vertices. Their index here is equal to their .index-member.
	static vector<Vertex*> all_verts;

	// specialized containers for each vertex type
	static list<StaticVertex> static_verts;
	static list<MotorizedVertex> motorized_verts;
	static list<DynamicVertex> dynamic_verts;
	static vector<int> ordered_dymanic_indices; // ordered by dependence
	static int num_vertices;

	// data preparation

	void init() {
		// TODO: garbage collection?

		all_verts = vector<Vertex*>();

		static_verts = list<StaticVertex>();
		motorized_verts = list<MotorizedVertex>();
		dynamic_verts = list<DynamicVertex>();
		ordered_dymanic_indices = vector<int>();
		num_vertices = 0;
	}

	int add_static_vertex(float x, float y) {
		int new_index = num_vertices++;
		StaticVertex new_vert = StaticVertex(x, y, new_index);
		static_verts.push_back(new_vert);
		all_verts.push_back(&(*--static_verts.end())); // inserted at new_index
		return new_index;
	}

	int add_motorized_vertex(float x, float y, int motor_vertex, float distance_to_motor) {
		int new_index = num_vertices++;
		MotorizedVertex new_vert = MotorizedVertex(x, y, motor_vertex, distance_to_motor, new_index);
		motorized_verts.push_back(new_vert);
		all_verts.push_back(&(*--motorized_verts.end())); // inserted at new_index
		return new_index;
	}

	int add_dynamic_vertex(float x, float y) {
		int new_index = num_vertices++;
		DynamicVertex new_vert = DynamicVertex(x, y, new_index);
		dynamic_verts.push_back(new_vert);
		all_verts.push_back(&(*--dynamic_verts.end())); // inserted at new_index
		return new_index;
	}

	void add_edge(int index_1, int index_2) {
		all_verts[index_1]->edges.push_back(index_2);
		all_verts[index_2]->edges.push_back(index_1);
	}

	bool prepare_simulation() {
		// order dynamic vertices by dependence
		int sorted = 0;
		vector<vector<int>> dependencies = vector<vector<int>>(num_vertices, vector<int>());
		list<int> ready = list<int>();
		for (StaticVertex s_vert : static_verts) {
			for (int adj : s_vert.edges) {
				if (all_verts[adj]->type == DYNAMIC) {
					dependencies[adj].push_back(s_vert.index);
					if (dependencies[adj].size() == 2) {
						ready.push_back(adj);
					}
				}
			}
		}
		for (MotorizedVertex m_vert : motorized_verts) {
			for (int adj : m_vert.edges) {
				if (all_verts[adj]->type == DYNAMIC) {
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
			
			auto dyn = find_if(dynamic_verts.begin(), dynamic_verts.end(),
				[current](DynamicVertex d) {return d.index == current; });
				
			//set dependants
			dyn->dependant_i = dependencies[current][0];
			dyn->dependant_j = dependencies[current][1];
			Vector2f k(dyn->initial_x, dyn->initial_y);
			Vector2f dependant_i(all_verts[dyn->dependant_i]->initial_x, all_verts[dyn->dependant_i]->initial_y);
			Vector2f dependant_j(all_verts[dyn->dependant_j]->initial_x, all_verts[dyn->dependant_j]->initial_y);
			Vector2f k_to_i = dependant_i - k;
			Vector2f k_to_j = dependant_j - k;
			dyn->distance_to_i = k_to_i.norm();
			dyn->distance_to_j = k_to_j.norm();

			// i -> j -> k must traverse the triangle counter-clockwise to ensure the correct orientation.
			// therefore, switch if triangle-normal is wrong.

			if (Vector3f(k_to_i.x(), k_to_i.y(), 0).cross(Vector3f(k_to_j.x(), k_to_j.y(), 0)).z() < 0) {
				int tempindex = dyn->dependant_i;
				dyn->dependant_i = dyn->dependant_j;
				dyn->dependant_j = tempindex;

				float tempdist = dyn->distance_to_i;
				dyn->distance_to_i = dyn->distance_to_j;
				dyn->distance_to_j = tempdist;
			}

			sorted++;
			for (int adj : all_verts[current]->edges) {
				if (all_verts[adj]->type == DYNAMIC) {
					dependencies[adj].push_back(all_verts[current]->index);
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
		for (auto it = motorized_verts.begin(); it != motorized_verts.end(); it++) {
			if (it->index == vertex_index) {
				it->current_rotation = rotation;
				return;
			}
		}
	}

	// simulation
	void get_simulated_positions(float* x_output_array, float* y_output_array) {

		// static
		for (StaticVertex s_vert : static_verts) {
			x_output_array[s_vert.index] = s_vert.initial_x;
			y_output_array[s_vert.index] = s_vert.initial_y;
		}

		// motorized
		for (MotorizedVertex m_vert : motorized_verts) {
			Vector2f motor_position(x_output_array[m_vert.motor_vertex], y_output_array[m_vert.motor_vertex]);

			Rotation2D rot(m_vert.current_rotation);
			Vector2f rotated_position = rot.toRotationMatrix() * Vector2f(m_vert.distance_to_motor, 0) + motor_position;

			x_output_array[m_vert.index] = rotated_position.x();
			y_output_array[m_vert.index] = rotated_position.y();
		}

		// dynamic
		for (DynamicVertex d_vert : dynamic_verts) {


			Vector2f i(x_output_array[d_vert.dependant_i], y_output_array[d_vert.dependant_i]);
			Vector2f j(x_output_array[d_vert.dependant_j], y_output_array[d_vert.dependant_j]);
			float dist_ik = d_vert.distance_to_i;
			float dist_jk = d_vert.distance_to_j;
			float dist_ij = (i - j).norm();
			float phi = acos(
				(dist_ij * dist_ij + dist_ik * dist_ik - dist_jk * dist_jk)
				/ (2 * dist_ij * dist_ik)
			);

			Rotation2D phi_rotation(phi);
			Vector2f k = phi_rotation.toRotationMatrix() * (dist_ik * (j - i) / (j - i).norm()) + i;
			x_output_array[d_vert.index] = k.x(); y_output_array[d_vert.index] = k.y();
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

}
