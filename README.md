# 2D Procedural Map Generation

This system builds maps by connecting modules through doors in a valid way, guaranteeing full connectivity, a single entrance and exit, and no open doors.

<p align="center">
  <img src="Gifs/MapGeneration.gif" width="600" />
</p>

## Module creation and configuration

Modules are configurable Prefabs set up from the editor. Each module must contain:

1. A root object with a *SpriteRenderer* and the `Module.cs` component.
2. As many child objects as doors the sprite has, placed in their corresponding positions and each with the `Door.cs` component.

To speed up creation, `Module.cs` includes a button in the inspector that automatically configures the serialized variables (collider, door list, and the orientation of each door).

The system **does not automatically detect which doors are entrances or exits**, so these must be manually marked from the inspector.

<p align="center">
  <img src="Gifs/ModuleCreation.gif" width="600" />
</p>

## Seed system

The system allows the use of a **reproducible seed**, generating exactly the same map when using the same value.

<p align="center">
  <img src="Gifs/Seed.gif" width="600" />
</p>

## How to generate a map

1. Open the `SampleScene`.
2. Select the `MapGenerator` GameObject.
3. Adjust the parameters from the Inspector.
4. Press **Generate Map**.

Each generation clears the previous map before creating a new one.

## Configurable parameters

From the `MapGenerator` component:

- **Module Library** (`ScriptableObject`): Library containing all the modules that the algorithm can use to generate the map.
- **Max Modules** (`int`): Maximum number of modules to generate.
- **Use Random Seed**  (`bool`): If enabled, the map is generated randomly.
- **Seed**  (`int`): If **Use Random Seed** is disabled, this value will be used as the seed for map generation.
- **Current Seed**  (`int`): Seed value used in the current generation. It is public so it can be easily copied.

## General algorithm flow

1. An initial module (Entrance) is instantiated.
2. Unconnected doors are connected with new modules.
3. For each door:
   - Candidate modules that have doors compatible with the orientation of the unconnected door are searched.
   - A random candidate is chosen.
   - The candidate is checked to ensure it meets the following requirements:
     - Does not collide with other already generated modules.
     - Does not block future map expansion.
     - The number of doors it generates does not force exceeding the maximum number of allowed modules.
     - Does not block other already created doors from being connected.
   - If the candidate does not meet any of these requirements, another candidate is chosen.
4. Step 2 is repeated until the maximum number of modules is reached.

## Limitations
If the module library does not contain closing modules (those with only one entrance) and small 1x1 modules, the algorithm tends to fail frequently.
