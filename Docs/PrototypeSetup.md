# Dagon Prototype Setup

This is the shortest path to getting the first vertical slice moving in Unity.

## Current Shortcut

There is now a runtime bootstrap in `Assets/Scripts/Bootstrap/PrototypeSceneBootstrap.cs`.

If you open the project and press Play in the sample scene, it will build a simple prototype arena automatically:

- perspective camera
- black mire ground
- sailor player
- imported sailor sprite
- roaming mire wretches using the imported mire sprite

This is the fastest way to test movement and scene feel before building prefabs manually.

## Scene Setup

1. Open `Assets/Scenes/SampleScene.unity`.
2. Keep the main camera as a perspective camera.
3. Rotate the camera to a fixed isometric angle and add `FollowCameraRig`.
4. Create a ground plane for the black mire.
5. Create a `Player` GameObject at the origin and tag it `Player`.
6. Add `PlayerMover`, `HarpoonLauncher`, `Health`, and `CorruptionMeter` to the player.
7. Create a child visual object under the player and add `BillboardSprite`.
8. Assign the camera transform as the `movementReference` on `PlayerMover`.
9. Create a projectile prefab with a trigger collider and `HarpoonProjectile`.
10. Assign that prefab to `HarpoonLauncher`.
11. Create an enemy prefab with `SimpleEnemyChaser`, `Health`, a trigger collider, and optional `ContactDamage`.
12. Add a child visual object with `BillboardSprite` to the enemy prefab.

## Input Setup

Recommended bindings from the existing input actions:

- `Move` -> `PlayerMover.moveAction`
- `Look` -> `PlayerMover.lookAction` if you want right-stick aiming later

If you do not assign actions, `PlayerMover` falls back to keyboard movement and mouse aim.

## Rendering Notes

- Use `Point (no filter)` on imported sprites.
- Disable mip maps for sprite textures.
- Start with a restrained perspective field of view to avoid making small sprites unreadable.
- Keep the world simple until scale and readability feel right.

## First Test Goal

You should be able to:

- move the sailor around a flat plane
- face the mouse with a billboarded sprite
- auto-fire harpoons
- damage a simple chasing enemy
