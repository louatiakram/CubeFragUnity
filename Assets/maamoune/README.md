# Maamoune - Chain Physics Simulation (Usage Guide)

This simulation models a realistic chain with adjustable detachment and energy transfer.

## Controls
- Open your Unity scene containing the chain setup.
- Configure ChainPhysicsSystem
-  the **ChainPhysicsSystem** component on your GameObject. Adjust Inspector parameters like chain length, link mass, spacing, gravity, detachment threshold, and `alpha` (for energy transfer).
- Press Play right away.
- Arrow keys: Move the top cube (anchor).
- W/A/S/D: Move the bottom cube (weight), if attached.
- Spacebar: Detach the bottom cube with energy transfer.
- R: Reset the chain and cubes.

## Inspector Paramaeters : 
- Number Of Links: How many segments in the chain.
- Link Mass / Size: Change heaviness and size.
- Link Spacing: Gaps between links.
- Gravity: -9.81 for normal physics.
- Stiffness: Higher resists stretch more (Constraint Stiffness).
- Max Stretch Before Detach: Lower = easier to break.
- Alpha: Energy transfer on detach; 0=none, 1=realistic.
- Cube Energy Ratio: Controls force given to the falling cube.


## Parameters (Change in Inspector)

- **Chain**: Length, mass, size, link spacing
- **Physics**: Gravity, air damping
- **Constraints**: Stiffness (K), damping (B), stretch threshold for detachment
- **Energy Transfer**: `alpha` for rebound realism, `cubeEnergyRatio` for impulse split
- **Visualization**: Link/constraint colors, debug info toggle

## Features

- Detachment triggers when chain stretch exceeds the threshold or Spacebar is pressed.
- Energy split and rebound is controlled by `alpha` and `cubeEnergyRatio`.
- Gizmos show chain state; GUI displays live physics info.

---

Tune parameters to test chain behavior; interact with cubes using the controls.
