using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>Initializes the shared movement rig when a supported test scene loads.</summary>
public static class StartSceneLocomotionBootstrap
{
    private const string StartScenePath = "Assets/Project/Scenes/Start.unity";
    private const string NightScenePath = "Assets/Project/Scenes/Start Night.unity";
    private const string StreetScenePath = "Assets/Project/Scenes/Test Street.unity";
    private const string DaylightStreetScenePath = "Assets/Project/Scenes/Test Street Daylight.unity";
    private const string NightStreetScenePath = "Assets/Project/Scenes/Test Street Night.unity";
    private const string UnlitStreetScenePath = "Assets/Project/Scenes/Test Street Night with no Light.unity";
    private const string LowLightStreetScenePath = "Assets/Project/Scenes/Test Street Night with Low Light.unity";

    private const string StartTestScenePath = SceneBucketManager.StudyStartPath;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneLoads()
    {
        SceneManager.sceneLoaded -= Install;
        SceneManager.sceneLoaded += Install;
    }

    private static void Install(Scene scene, LoadSceneMode loadMode)
    {
        var scenePath = scene.path;
        if (scenePath != StartScenePath && scenePath != NightScenePath && scenePath != StreetScenePath &&
            scenePath != DaylightStreetScenePath && scenePath != NightStreetScenePath && scenePath != UnlitStreetScenePath &&
            scenePath != LowLightStreetScenePath && scenePath != StartTestScenePath)
            return;

        OVRCameraRig cameraRig = null;
        OVRManager ovrManager = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (cameraRig == null) cameraRig = root.GetComponentInChildren<OVRCameraRig>(true);
            if (ovrManager == null) ovrManager = root.GetComponentInChildren<OVRManager>(true);
        }
        if (cameraRig == null)
        {
            Debug.LogError("Scene locomotion could not find OVRCameraRig in " + scenePath);
            return;
        }

        var player = cameraRig.gameObject;
        var characterController = player.GetComponent<CharacterController>();
        if (characterController == null) characterController = player.AddComponent<CharacterController>();
        characterController.radius = 0.25f;
        characterController.height = 1.7f;
        characterController.center = new Vector3(0f, 0.85f, 0f);
        characterController.stepOffset = 0.25f;
        characterController.slopeLimit = 50f;
        characterController.skinWidth = 0.03f;
        characterController.minMoveDistance = 0f;

        var desktopCamera = CreateDesktopCamera(player.transform);
        var locomotion = player.GetComponent<ExperimentLocomotion>();
        if (locomotion == null) locomotion = player.AddComponent<ExperimentLocomotion>();
        locomotion.Initialize(cameraRig, ovrManager, desktopCamera, characterController, ExperimentControlMode.VrController);
        if (scenePath == StartTestScenePath)
        {
            locomotion.enabled = false;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var trigger in root.GetComponentsInChildren<SceneTriggerZone>(true))
                    trigger.gameObject.SetActive(false);
            }
            player.AddComponent<StudyStartMenu>().Initialize(cameraRig.centerEyeAnchor);
        }
        Debug.Log("Scene locomotion initialized in VrController mode: " + scenePath, locomotion);
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
            cameraObject.AddComponent<UniversalAdditionalCameraData>();
        if (cameraObject.GetComponent<AudioListener>() == null) cameraObject.AddComponent<AudioListener>();
        return camera;
    }
}
