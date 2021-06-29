#include "pch.h" // use stdafx.h in Visual Studio 2017 and earlier
#include <utility>
#include <limits.h>
#include "SymboDLL.h"
#include <fstream>
using namespace std;

#include "Linkage_Data.h"

// Eigen
#include <Eigen/Core>
#include <Eigen/Geometry>
using namespace Eigen;

// cppoptlib
#include <cppoptlib/meta.h>
#include <cppoptlib/problem.h>
#include <cppoptlib/solver/bfgssolver.h>
#include <cppoptlib/solver/gradientdescentsolver.h>
using namespace cppoptlib;

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
	static vector<pair<int, int>> edges;
	static int num_vertices = false;
	static bool is_initialized;


	// --- data preparation ---

	void init() {
		if (is_initialized) { // memory management
			all_verts.clear();
			static_verts.clear();
			motorized_verts.clear();
			dynamic_verts.clear();
			ordered_dymanic_indices.clear();
			edges.clear();
		}

		all_verts = vector<Vertex*>();
		static_verts = list<StaticVertex>();
		motorized_verts = list<MotorizedVertex>();
		dynamic_verts = list<DynamicVertex>();
		ordered_dymanic_indices = vector<int>();
		edges = vector<pair<int, int>>();
		num_vertices = 0;
		is_initialized = true;
	}

	int add_static_vertex(float x, float y) {
		int new_index = num_vertices++;
		StaticVertex new_vert = StaticVertex(x, y, new_index);
		static_verts.push_back(new_vert);
		all_verts.push_back(&(*--static_verts.end())); // inserted at new_index
		return new_index;
	}

	int add_motorized_vertex(float x, float y, int motor_vertex) {
		int new_index = num_vertices++;
		float distance_to_motor = (Vector2f(all_verts[motor_vertex]->initial_x, all_verts[motor_vertex]->initial_y)
			- Vector2f(x, y)).norm();
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
		edges.push_back(pair<int, int>(index_1, index_2));
	}

	bool prepare_simulation() {
		// order dynamic vertices by dependence
		
		int sorted = 0;
		// tracks for each vertex what other vertices can be used for symbolic kinematics
		vector<vector<int>> dependencies = vector<vector<int>>(num_vertices, vector<int>());
		list<int> ready = list<int>();
		for (StaticVertex s_vert : static_verts) {
			for (int adj : s_vert.edges) {
				if (all_verts[adj]->type == VertexType::DYNAMIC) {
					dependencies[adj].push_back(s_vert.index);
					if (dependencies[adj].size() == 2) {
						ready.push_back(adj);
					}
				}
			}
		}
		for (MotorizedVertex m_vert : motorized_verts) {
			for (int adj : m_vert.edges) {
				if (all_verts[adj]->type == VertexType::DYNAMIC) {
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
				if (all_verts[adj]->type == VertexType::DYNAMIC) {
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


	// --- control ---

	void set_motor_rotation(int vertex_index, float rotation) {
		for (auto it = motorized_verts.begin(); it != motorized_verts.end(); it++) {
			if (it->index == vertex_index) {
				it->current_rotation = rotation;
				return;
			}
		}
	}


	// --- simulation ---
	
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


	dual get_edge_length_gardient(const VectorXdual& edge_lengths, const int vert_index, const Vector2dual& target_pos) {
		
		// Step 1: simulate all the positions
		MatrixXdual positions(2, num_vertices); // x = coordinate, y = value

		// static
		for (StaticVertex s_vert : static_verts) {
			positions(0, s_vert.index) = s_vert.initial_x;
			positions(1, s_vert.index) = s_vert.initial_y;
		}
		
		// motorized
		for (MotorizedVertex m_vert : motorized_verts) {
			Vector2dual motor_position(positions(0, m_vert.motor_vertex), positions(1, m_vert.motor_vertex));
			
			dual current_rotation = dual(); current_rotation = m_vert.current_rotation;
			Rotation2D rot(current_rotation);
			dual distance_to_motor = dual(); distance_to_motor = m_vert.distance_to_motor;
			for (int i = 0; i < edges.size(); i++) {
				if ((edges[i].first == m_vert.index && edges[i].second == m_vert.motor_vertex)
					|| (edges[i].first == m_vert.motor_vertex && edges[i].second == m_vert.index)) {
					distance_to_motor = edge_lengths[i];
				}
			}
			Vector2dual horizontal(distance_to_motor, 0);
			 
			Vector2dual rotated_position = Vector2dual(
				(MatrixXdual(rot.toRotationMatrix()) * horizontal + motor_position));

			positions(0, m_vert.index) = rotated_position.x();
			positions(1, m_vert.index) = rotated_position.y();
		}
		
		// dynamic
		for (DynamicVertex d_vert : dynamic_verts) {

			
			Vector2dual i(positions(0, d_vert.dependant_i), positions(1, d_vert.dependant_i));
			Vector2dual j(positions(0, d_vert.dependant_j), positions(1, d_vert.dependant_j));
			dual dist_ik = dual(); dist_ik = d_vert.distance_to_i;
			dual dist_jk = dual();  dist_jk = d_vert.distance_to_j;
			dual dist_ij = Vector2dual(i - j).norm();
			int index_k = d_vert.index, index_i = d_vert.dependant_i, index_j = d_vert.dependant_j;
			
			// quick-n-dirty solution
			for (int e = 0; e < edges.size(); e++) {
				if ((edges[e].first == index_i && edges[e].second == index_k)
					|| (edges[e].first == index_k && edges[e].second == index_i)) {
					dist_ik = edge_lengths[e];
					break;
				}
			}
			for (int e = 0; e < edges.size(); e++) {
				if ((edges[e].first == index_j && edges[e].second == index_k)
					|| (edges[e].first == index_k && edges[e].second == index_j)) {
					dist_jk = edge_lengths[e];
					break;
				}
			}
			for (int e = 0; e < edges.size(); e++) {
				if ((edges[e].first == index_i && edges[e].second == index_j)
					|| (edges[e].first == index_j && edges[e].second == index_i)) {
					dist_ij = edge_lengths[e];
					break;
				}
			}
			dual phi = dual(); phi = acos(
				(dist_ij * dist_ij + dist_ik * dist_ik - dist_jk * dist_jk)
				/ (2 * dist_ij * dist_ik)
			);

			Rotation2D phi_rotation(phi);
			Vector2dual k = Vector2dual(MatrixXdual(phi_rotation.toRotationMatrix()) * dist_ik * (j - i) / Vector2dual(j - i).norm() + i);
			positions(0, d_vert.index) = k.x(); positions(1, d_vert.index) = k.y();
		}
		
		// Step 2: check current error
		Vector2dual current_position_of_vertex(positions(0, vert_index), positions(1, vert_index));
		dual error = dual(); error = Vector2dual(current_position_of_vertex - target_pos).norm();
		return error;
	}


	void get_edge_length_gradients_for_target_position(
		int vertex_index, float x, float y,
		float* first_end, float* second_end, float* gradient_for_edge)
	{
		
		VectorXdual edge_lengths = VectorXdual(edges.size());
		for (int i = 0; i < edges.size(); i++) {
			Vector2f v1(all_verts[edges[i].first]->initial_x, all_verts[edges[i].first]->initial_y);
			Vector2f v2(all_verts[edges[i].second]->initial_x, all_verts[edges[i].second]->initial_y);
			edge_lengths(i) = (v1 - v2).norm();
		}

		dual magnitude;
		VectorXd g = gradient(
			get_edge_length_gardient,
			wrt(edge_lengths),
			at(edge_lengths, vertex_index, Vector2dual(x, y)),
			magnitude);

		for (int i = 0; i < edges.size(); i++) {
			first_end[i] = edges[i].first;
			second_end[i] = edges[i].second;
			gradient_for_edge[i] = g(i);
		}

	}


	// --- optimization ---

	struct grad_and_objective { VectorXd grad; double objective; };
	grad_and_objective gradient_and_objective_for_target_position(
		const VectorXd& input_edge_lengths,
		int vertex_index, float x, float y)
	{
		VectorXdual edge_lengths = VectorXdual(input_edge_lengths.size());
		for (int i = 0; i < input_edge_lengths.size(); i++) {
			edge_lengths(i) = input_edge_lengths(i);
		}

		dual magnitude;
		VectorXd g = gradient(
			get_edge_length_gardient,
			wrt(edge_lengths),
			at(edge_lengths, vertex_index, Vector2dual(x, y)),
			magnitude);

		VectorXd grad = VectorXd(input_edge_lengths.size());
		for (int i = 0; i < g.size(); i++) {
			grad(i) = - g(i);
		}
		grad_and_objective ret;
		ret.grad = grad;
		ret.objective = magnitude.val;
		return ret;
	}

	template<typename T> class EdgeLengthMinimizer : public Problem<T> {
	public:
		using typename Problem<T>::TVector;

		int target_vert = 0;
		Vector2d target_position;

		void set_target(int vertex_index, float target_x, float target_y) {
			target_vert = vertex_index;
			target_position = Vector2d(target_x, target_y);
		}


		// objective function
		T value(const TVector& x) {
			auto [grad, obj] = gradient_and_objective_for_target_position(x, target_vert, target_position.x(), target_position.y());
			return obj;
		}

		// optional override of gradient (we calculate it ourselves)
		void gradient(const TVector& x, TVector& grad) {
			auto [gradients, obj] = gradient_and_objective_for_target_position(x, target_vert, target_position.x(), target_position.y());
			for (int i = 0; i < gradients.size(); i++) {
				grad[i] = gradients(i);
			}
		}
	};


	bool optimize_for_target_location(int vertex_index, float x, float y) {
		// ---------- DEBUG -------------
		ofstream out("unity_symbo_dll_cout.txt"); cout.rdbuf(out.rdbuf());
		ofstream err("unity_symbo_dll_cerr.txt"); cerr.rdbuf(err.rdbuf());
		cout << "writing to cout" << endl;
		cerr << "writing to cerr" << endl;
		// ---------- DEBUG -------------
		
		VectorXd edge_lengths = VectorXd(edges.size());
		for (int i = 0; i < edges.size(); i++) {
			Vector2d v1(all_verts[edges[i].first]->initial_x, all_verts[edges[i].first]->initial_y);
			Vector2d v2(all_verts[edges[i].second]->initial_x, all_verts[edges[i].second]->initial_y);
			edge_lengths(i) = (v1 - v2).norm();
		}
		for (auto d = dynamic_verts.begin(); d != dynamic_verts.end(); d++) { // ugly solution to get up-to-date lengths
			for (int i = 0; i < edges.size(); i++) {
				if (edges[i].first == d->index && edges[i].second == d->dependant_i || edges[i].second == d->index && edges[i].first == d->dependant_i) {
					edge_lengths[i] = d->distance_to_i;
				}
				if (edges[i].first == d->index && edges[i].second == d->dependant_j || edges[i].second == d->index && edges[i].first == d->dependant_j) {
					edge_lengths[i] = d->distance_to_j;
				}
			}
		}
		
		/*EdgeLengthMinimizer<double> f;
		GradientDescentSolver<EdgeLengthMinimizer<double>> solver;
		
		f.set_target(vertex_index, x, y);

		bool is_gradient_valid = f.checkGradient(edge_lengths);
		if (!is_gradient_valid) return false;

		solver.minimize(f, edge_lengths);*/
		
		auto [grad, obj] = gradient_and_objective_for_target_position(edge_lengths, vertex_index, x, y);

		for (auto d = dynamic_verts.begin(); d != dynamic_verts.end(); d++) {
			for (int i = 0; i < edges.size(); i++) {
				if ((edges[i].first == d->index && edges[i].second == d->dependant_i)
					|| edges[i].second == d->index && edges[i].first == d->dependant_i) {
					d->distance_to_i = edge_lengths(i) + grad(i) * 0.01 * min(obj, 1.0);
				}
				if ((edges[i].first == d->index && edges[i].second == d->dependant_j)
					|| edges[i].second == d->index && edges[i].first == d->dependant_j) {
					d->distance_to_j = edge_lengths(i) + grad(i) * 0.01 * min(obj, 1.0);
				}
			}
		}
		return true;
	}

}
