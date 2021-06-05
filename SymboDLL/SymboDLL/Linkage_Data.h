#pragma once

#include <vector>
using namespace std;

namespace Symbo {

	enum class VertexType { STATIC, MOTORIZED, DYNAMIC };

	class Vertex {
	public:
		int index;
		float initial_x, initial_y;
		vector<int> edges = vector<int>();
		VertexType type;
	};

	class StaticVertex : public Vertex {
	public:
		StaticVertex(float x, float y, int index) {
			this->initial_x = x;
			this->initial_y = y;
			this->index = index;
			this->type = VertexType::STATIC;
		}
	};

	class MotorizedVertex : public Vertex {
	public:
		int motor_vertex;
		float distance_to_motor;
		float current_rotation;
		MotorizedVertex(float x, float y, int motor_vertex, float distance_to_motor, int index) {
			this->initial_x = x;
			this->initial_y = y;
			this->motor_vertex = motor_vertex;
			this->distance_to_motor = distance_to_motor;
			this->current_rotation = 0;
			this->index = index;
			this->type = VertexType::MOTORIZED;
		}
	};

	class DynamicVertex : public Vertex {
	public:
		int dependant_i, dependant_j;
		float distance_to_i, distance_to_j;
		DynamicVertex(float x, float y, int index) {
			this->initial_x = x;
			this->initial_y = y;
			this->index = index;
			this->type = VertexType::DYNAMIC;
			// these members are properly set as part of prepare_simulation()
			this->dependant_i = -1;
			this->dependant_j = -1;
			this->distance_to_i = 0;
			this->distance_to_j = 0;
		}
	};

}
