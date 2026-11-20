using UnityEngine;

/// <summary>Initializes only the study rig explicitly assigned in this scene.</summary>
[DefaultExecutionOrder(-500)]
[DisallowMultipleComponent]
public sealed class GroupStudyPlayerSetup : MonoBehaviour
{
    [SerializeField] private OVRCameraRig cameraRig;
    [SerializeField] private OVRManager manager;
    [SerializeField] private Camera desktopCamera;
    [SerializeField] private ExperimentLocomotion locomotion;
    [SerializeField] private CharacterController characterController;
    private AudioListener[] listeners;

    private void Start()
    {
        if (cameraRig == null || desktopCamera == null || locomotion == null || characterController == null)
        {
            Debug.LogError("Study player setup has missing scene references.", this);
            enabled = false;
            return;
        }
        locomotion.Initialize(cameraRig, manager, desktopCamera, characterController, ExperimentControlMode.DesktopGamepad);
        listeners = cameraRig.GetComponentsInChildren<AudioListener>(true);
        UpdateListeners();
    }

    private void LateUpdate() => UpdateListeners();

    private void UpdateListeners()
    {
        if (listeners == null) return;
        var selectedCamera = locomotion.ControlMode == ExperimentControlMode.DesktopGamepad
            ? desktopCamera : cameraRig.centerEyeAnchor.GetComponent<Camera>();
        foreach (var listener in listeners)
            listener.enabled = selectedCamera != null && selectedCamera.isActiveAndEnabled && listener.gameObject == selectedCamera.gameObject;
    }
}
