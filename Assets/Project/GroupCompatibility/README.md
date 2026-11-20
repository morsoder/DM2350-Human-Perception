# Group scene compatibility repairs

These resources support the 48 scenes in Assets/Project/Scenes/Group1, Group2 and Group3.

- InputSystemUIInputModule replaces the old StandaloneInputModule because this project uses the new Input System. Group UI Controls and its persistent action references support pointer, keyboard, gamepad and tracked-device UI input.
- The two URP material copies replace built-in Standard and Legacy Diffuse materials in scene renderers. Original shared materials and prefabs remain unchanged.
- Scene geometry, feature visibility, lighting, camera settings, colliders and the previous highest-detail model selections are preserved. The oversized goBack covering plane remains disabled.

This repair does not install the study player rig or recorder in the group scenes, or set up the three groups' lighting conditions.