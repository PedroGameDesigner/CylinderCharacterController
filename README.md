# Cylinder Character Controller
An implementation of a Cylinder collider for Unity and a character controller build using said collider. 
Implemented with 3D Platformer in mind.
![](ReadmeCapture.png?raw=true "Capture")

## Description
This projects add a **cylinder collider* to Unity, along with a classes that can be use to build a character controller.
The features of this project are:
* Collision detection based on raycasting.
* Climbing and descending slopes and steps.
* Slide down too steep slopes.

## Getting Started
###Instalation
* Download/clone project on a local folder.
* Open project with Unity (version >= 2021)

### Class Breakdown
**CylinderCollider** is the class that defines the dimensions of the collider and the configuration of the ray to be cast. 

**CharacterController** is the class that, using the collision detection of CylinderCollider, process the character velocity to make it interact correctly with the environment.

**CharacterController** an abstract class. The user must extend this class and add functionality to process the players input into the speed.

To work, a character gameobject must have one of each component.

## TO DO
* Ride moving platform
* Different floor frictions
* Configuration by ScriptableObject
* More complete character controller for the demo

## Know bugs
* Sometimes the game frame rate drops notably.