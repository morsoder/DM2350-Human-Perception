using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Installs the locomotion prototype in the Start, Start Night and Test Street scenes.
/// Start.unity itself stays unchanged, so removing this script restores the
/// original scene behaviour.
/// </summary>
public static class StartSceneLocomotionBootstrap
{
    private const string StartScenePath = "Assets/Project/Scenes/Start.unity";
    private const string NightScenePath = "Assets/Project/Scenes/Start Night.unity";
    private const string StreetScenePath = "Assets/Project/Scenes/Test Street.unity";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        var scenePath = SceneManager.GetActiveScene().path;
        if (scenePath != StartScenePath && scenePath != NightScenePath && scenePath != StreetScenePath) return;

        var cameraRig = Object.FindAnyObjectByType<OVRCameraRig>(FindObjectsInactive.Include);
        if (cameraRig == null)
        {
            Debug.LogError("Start scene locomotion could not find OVRCameraRig.");
            return;
        }

        var player = cameraRig.gameObject;
        var characterController = player.GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = player.AddComponent<CharacterController>();
        }

        characterController.radius = 0.25f;
        characterController.height = 1.7f;
        characterController.center = new Vector3(0f, 0.85f, 0f);
        characterController.stepOffset = 0.25f;
        characterController.slopeLimit = 50f;
        characterController.skinWidth = 0.03f;
        characterController.minMoveDistance = 0f;

        var desktopCamera = CreateDesktopCamera(player.transform);
        var ovrManager = Object.FindAnyObjectByType<OVRManager>(FindObjectsInactive.Include);
        // Reuse a scene-authored component so Inspector settings survive Play sessions.
        var locomotion = player.GetComponent<ExperimentLocomotion>();
        if (locomotion == null) locomotion = player.AddComponent<ExperimentLocomotion>();
        locomotion.Initialize(
            cameraRig,
            ovrManager,
            desktopCamera,
            characterController,
            ExperimentControlMode.DesktopGamepad);

        Debug.Log("Start scene locomotion prototype installed in DesktopGamepad mode.", locomotion);
    }

    private static Camera CreateDesktopCamera(Transform parent)
    {
        var existing = parent.Find("DesktopCameraRig");
        var cameraObject = existing != null ? existing.gameObject : new GameObject("DesktopCameraRig");
        cameraObject.transform.SetParent(parent, false);
        cameraObject.transform.localPosition = new Vector3(0f, 1.65f, 0f);
        cameraObject.transform.localRotation = Quaternion.identity;

        var camera = cameraObject.GetComponent<Camera>();
        if (camera == null) camera = cameraObject.AddComponent<Camera>();
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 2000f;
        camera.fieldOfView = 70f;

        if (cameraObject.GetComponent<UniversalAdditionalCameraData>() == null)
        {
            cameraObject.AddComponent<UniversalAdditionalCameraData>();
        }

        if (cameraObject.GetComponent<AudioListener>() == null)
        {
            cameraObject.AddComponent<AudioListener>();
        }

        return camera;
    }
}
