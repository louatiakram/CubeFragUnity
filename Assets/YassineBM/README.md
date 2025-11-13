# YassineBM - Fragment Sphere Physics Simulation

This folder contains a physics-based simulation system for fragmenting spheres that interact with each other and the environment.

## Getting Started

### 1. Open the Scene
Navigate to and open the **YassineSphereScene** scene in your Unity project.

### 2. Configure the SceneSetup GameObject
Once the scene is open, locate the **SceneSetup** GameObject in the hierarchy and configure it in the Inspector.

## SceneSetup Configuration

The SceneSetup component provides several configuration options:

### Sphere Configuration
- **Fragments Per Sphere**: Number of fragments that make up each sphere
- **Sphere Radius**: The radius of each fragment sphere
- **Fragment Size**: The size of individual fragments

### Sphere Spawning
- **Number Of Spheres**: How many fragment spheres to create in the scene
- **Spawn Area Size**: The dimensions of the area where spheres will randomly spawn
- **Spawn Center**: The center point of the spawn area

### Physics
- **Gravity**: The gravitational force applied to fragments (default: 9.81 m/s²)
- **Ground Level**: The height of the ground/platform
- **Fragment Mass**: Mass of each individual fragment
- **Restitution**: Bounciness of fragments (0 = no bounce, 1 = perfect bounce)

### Collision Settings
- **Enable Fragment Collisions**: Toggle collision detection between fragments
- **Create Spring Constraints**: Create spring constraints between nearby fragments to maintain sphere structure
- **Initial Constraint Distance**: Distance for spring constraint creation

### Initial Velocity (Optional)
- **Apply Random Velocity**: Give fragments random initial velocities
- **Max Initial Velocity**: Maximum speed for random initial velocities

## Features

- **Spherical Fragment Distribution**: Uses the Fibonacci sphere algorithm for even fragment distribution
- **Collision System**: Handles fragment-to-fragment and sphere-to-sphere collisions
- **Spring Constraints**: Optional constraints to keep fragments connected
- **Ground Collision**: Fragments bounce off the ground platform based on restitution
- **Physics Integration**: Custom physics simulation with gravity and damping

## Runtime Display

Once running, the scene displays on-screen statistics:
- Number of fragment spheres
- Total fragment count
- Current gravity settings
- Collision status
- Restitution level
- Fragment mass

## Tips

- Adjust the restitution value to control how bouncy the simulation feels
- Increase spring constraints for a more cohesive sphere structure
- Experiment with different spawn configurations to see various interactions
- Monitor performance by adjusting the number of fragments and spheres based on your target platform

