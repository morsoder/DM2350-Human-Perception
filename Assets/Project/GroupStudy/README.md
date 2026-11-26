# Group study scenes

The APK starts in StartTestScene.unity. Its legacy direct-to-Group-1 trigger is disabled while the menu is active. The operator selects Daylight, Low-light night, or Bright night with the Quest left stick and confirms with A on the right controller. During editor testing, keys 1, 2, and 3 start the same groups. The first scene is selected at random. After the last choice, the application returns to StartTestScene and a subsequent group selection creates a fresh randomized pool.

Configured scenes: all 48 study scenes — 16 Group1 daylight, 16 Group2 low-night and 16 Group3 bright-night scenes.

The Group1 Daylight Study root contains the player rig, scene-specific daylight settings and route recorder. Original building, greenery and street-object selections are retained. The scene reuses the existing StreetDaylight sky, pipeline, post-processing and lit shop materials without changing those assets. Original artificial light components are off; existing kiosk windows/signs remain visibly lit. Daylight currently uses the same real-time sun/environment treatment as the approved test scene, with no new indirect-light bake.

Quest: The scene starts in VR controller mode. The Quest left stick moves the player, and the headset tracks head movement. Press F1 in the editor to test desktop controls and F2 to return to VR.

Desktop: WASD or Xbox left stick moves; hold right mouse button or use right stick to look. Movement speed is exposed on Group Study PlayerRig / ExperimentLocomotion (3 metres per second in these scenes). The player starts on the intersection asphalt at X 22.90, Y 2.17, Z 75.87, facing between the two streets. These controls are available after switching to desktop mode. Quest hardware validation is pending.

Recording: G / Xbox X / Quest left X toggles recording. Press a second time to stop and save a true overhead PNG. No on-screen help or recording panel is shown. Raw CSV/JSON export is disabled for this scene. Set the participant ID on the root's RouteRecorder before each session. Default output: Documents/DM2350_Results, with participant/group/scene/time in the PNG filename. Stop recording before leaving Play mode to save the PNG. Entering a left or right NextSceneTrigger saves any active recording before loading an unvisited scene from the same lighting group. Start recording manually for each scene.

GroupStudyPlayerSetup initializes only the explicitly assigned scene rig after its camera components have awakened. It does not register a global scene-loading hook. The old Test Street prototype scenes keep their existing configurations. All original scene geometry and prefab assets remain in place; the previously disabled covering plane and highest-detail model settings are preserved.

## Group2 — low night, streetlights only

All 16 scenes in Assets/Project/Scenes/Group2 use a Group2 Low Night Study root, with the same desktop/VR movement interfaces and manual PNG-only recording controls as Group1. Recorder group ID is G2; participant ID defaults to P001 and should be set before each participant's session. Movement speed remains 3 metres per second and the initial corner pose is shared with Group1.

These study maps contain 22 visible lamp fixtures, rather than the old test map's 24. New light positions are derived from each existing fixture's glass submesh. Eleven evenly distributed fixtures are enabled in every scene, with the others' light and luminous face disabled. No street fixtures are added or moved. Lamp intensity 90, range 34, outer angle 140 degrees and inner angle 65 degrees match the approved low-night prototype. Native old lamps/car lights are disabled to prevent duplicate lighting. Residential windows, shop windows, signs and other unmanaged emissive materials use non-emitting source materials or private off copies, including their contribution to baked lighting.

The approved refined night sky, fixed-exposure post-processing and Forward+ pipeline are reused without editing them. Moon intensity is 0.08 and the low ambient night baseline matches the previous low-light test scene. Each layout has its own baked indirect lightmaps and 264 baked lighting probes under GroupStudy/Group2Night/Bakes/<layout>. Original model assets are unchanged; private mesh copies supply lightmap UVs where needed, with original vertex positions verified. Geometry, object activation, collisions, disabled covering plane and highest-detail model selections remain as authored.

When changing lamp number, positions, brightness or emission, rebake the affected scene; changing only the lighting enum cannot update the stored indirect lighting. Desktop checks passed for all 16 scenes; actual Quest viewing and performance still require headset validation. Each group uses its own randomized sequence without repeats.


## Group3 — bright night, streetlights plus windows and shops

All 16 scenes in Assets/Project/Scenes/Group3 use a Group3 Bright Night Study root. All 22 existing street fixtures are illuminated. Lamp location, intensity, colour, cone angles, shadows, moon, ambient night light, sky, exposure, post-processing, movement speed and initial viewpoint match the corresponding Group2 layout. Only artificial-light availability and the intended window/shop illumination change.

Residential glow is applied only to existing, visible drape/window surfaces with centres between world Y5 and Y23. A fixed seed2350001 and a key based on the existing window's position rounded to centimetres and original material GUID select approximately27percent, using three warm-colour/intensity variants from the earlier prototype. The same physical window has the same selection across layouts. Solid facades stay solid; no window geometry is added. Existing fabric texture and UV mapping are reused for emission. Window counts differ naturally with the original facade configuration.

Only shops already present and visible in a layout are illuminated. Existing kiosk windows and signs use private emissive material copies retaining their original image textures. The approved prototype's entrance/spill lights are mapped through each actual shop model's transform. This gives zero, one or two lit shops according to the original street-object configuration; no extra shops are introduced. The existing shop models remain exterior/display models, with no new interior rooms.

The G3 lighting condition enables all street faces and configured residential/shop targets. Their indirect illumination is independently baked for each layout into GroupStudy/Group3Night/Bakes/<layout>. Each scene has its own lighting data and264 baked probes. New materials and20 verified UV mesh copies are private to Group3; original assets and Group1/Group2 lighting data remain untouched. Lighting changes require rebaking that layout.

Recorder group ID is G3, participant ID defaults to P001, overlays remain hidden and exported participant results are PNG only. The same G / Xbox X / Quest left X controls apply. Set the participant ID and stop recording before leaving Play. All16 scenes passed desktop camera, keyboard/gamepad locomotion and two independent PNG exports; real headset display/performance validation remains pending. The scene pool is built from enabled scene paths in the current lighting group.
