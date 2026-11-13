Ebba Street Scenes - environment migration
49 source scenes, preserving their folder structure under Assets/EbbaStreetScenes/Scenes.
Prepared for Unity 6000.6.8f1 and URP.
Source: D:/unity/Street walkthrough Ebba Rovig/upgraderat projekt

The original project is unchanged. This copy contains environments rather than the original experiment application.
Removed missing legacy scripts; excluded source C# scripts, native plugins and SteamVR packages.
Disabled source cameras, AudioListeners and AudioSources. Add the destination project's XR player when testing.
Custom source tags reset to Untagged; source UILayer (8) mapped to Default.
Supported materials converted with Unity's URP material upgrader. Remaining custom shaders are listed in migration-report.txt and may need manual adaptation.
All imported asset GUIDs regenerated consistently to avoid overriding existing destination assets.
No destination Packages, ProjectSettings or build scene list is replaced.
Lighting and appearance require review before use in a perception experiment.

Source already-unavailable references and excluded SteamVR prefab references were cleared. See source-unavailable-references.json. Removed missing prefab records: 78.

Validation: all 49 scenes open, zero missing scripts. There are 2443 MeshFilters without assigned meshes across all loaded scenes; the prepared source baseline has the same count (2443). This does not constitute a full visual or VR runtime test.
