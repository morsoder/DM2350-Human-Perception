using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

public enum ExperimentControlMode
{
    Disabled,
    DesktopGamepad,
    VrController
}

/// <summary>
/// Movement with a Quest controller or desktop input. OVRCameraRig owns head tracking.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public sealed class ExperimentLocomotion : MonoBehaviour
{
    [Header("Runtime mode")]
    [SerializeField] private ExperimentControlMode controlMode = ExperimentControlMode.VrController;
    [SerializeField] private bool allowDevelopmentModeShortcuts = true;
    [SerializeField] private bool showDesktopHelp = true;

    [Header("Movement")]
    [Min(0.1f), Tooltip("Movement speed in metres per second. Applies to keyboard, gamepad and VR locomotion.")]
    public float movementSpeed = 1.4f;
    [SerializeField, Min(0.1f)] private float acceleration = 5f;
    [SerializeField, Min(0.1f)] private float deceleration = 8f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField, Range(0f, 0.95f)] private float stickDeadZone = 0.15f;

    [Header("Desktop view")]
    [SerializeField, Min(1f)] private float gamepadLookSpeed = 110f;
    [SerializeField, Min(0.01f)] private float mouseLookScale = 0.08f;
    [SerializeField, Range(10f, 89f)] private float maximumPitch = 80f;

    private CharacterController _characterController;
    private OVRCameraRig _cameraRig;
    private OVRManager _ovrManager;
    private Camera _desktopCamera;
    private Camera[] _vrCameras;
    private bool[] _initialVrCameraStates;

    private Vector3 _spawnPosition;
    private Quaternion _spawnRotation;
    private Vector3 _horizontalVelocity;
    private float _verticalVelocity;
    private float _desktopPitch;
    private bool _paused;
    private bool _initialized;

    public ExperimentControlMode ControlMode => controlMode;
    public bool IsPaused => _paused;

    public void Initialize(
        OVRCameraRig cameraRig,
        OVRManager ovrManager,
        Camera desktopCamera,
        CharacterController characterController,
        ExperimentControlMode initialMode)
    {
        _cameraRig = cameraRig;
        _ovrManager = ovrManager;
        _desktopCamera = desktopCamera;
        _characterController = characterController;
        _spawnPosition = transform.position;
        _spawnRotation = transform.rotation;

        _vrCameras = _cameraRig != null
            ? _cameraRig.GetComponentsInChildren<Camera>(true)
            : new Camera[0];
        _initialVrCameraStates = new bool[_vrCameras.Length];
        for (var i = 0; i < _vrCameras.Length; i++)
        {
            _initialVrCameraStates[i] = _vrCameras[i].enabled;
        }

        _initialized = true;
        SetControlMode(initialMode);
    }

    private void Update()
    {
        if (!_initialized) return;

        HandleDevelopmentShortcuts();
        HandleResetAndPauseInput();
        UpdateCharacterControllerShape();

        if (_paused || controlMode == ExperimentControlMode.Disabled)
        {
            _horizontalVelocity = Vector3.MoveTowards(
                _horizontalVelocity,
                Vector3.zero,
                deceleration * Time.deltaTime);
            ApplyMovement(Vector3.zero);
            return;
        }

        var desiredDirection = Vector3.zero;
        switch (controlMode)
        {
            case ExperimentControlMode.DesktopGamepad:
                UpdateDesktopView();
                desiredDirection = ReadDesktopMovement();
                break;
            case ExperimentControlMode.VrController:
                desiredDirection = ReadVrControllerMovement();
                break;
        }

        var desiredVelocity = desiredDirection * movementSpeed;
        var rate = desiredVelocity.sqrMagnitude > _horizontalVelocity.sqrMagnitude
            ? acceleration
            : deceleration;
        _horizontalVelocity = Vector3.MoveTowards(
            _horizontalVelocity,
            desiredVelocity,
            rate * Time.deltaTime);

        ApplyMovement(_horizontalVelocity);
    }

    public void SetControlMode(ExperimentControlMode mode)
    {
        controlMode = mode;
        _paused = false;
        _horizontalVelocity = Vector3.zero;
        _verticalVelocity = 0f;

        var desktopMode = mode == ExperimentControlMode.DesktopGamepad;
        var vrMode = mode == ExperimentControlMode.VrController;

        if (_desktopCamera != null)
        {
            _desktopCamera.gameObject.SetActive(desktopMode);
        }

        if (_cameraRig != null)
        {
            _cameraRig.enabled = vrMode;
        }

        for (var i = 0; i < _vrCameras.Length; i++)
        {
            // DesktopCameraRig is parented under OVRCameraRig, so it is also
            // returned by GetComponentsInChildren. It is controlled above and
            // must not be disabled together with the tracked VR cameras.
            if (_vrCameras[i] == _desktopCamera) continue;
            _vrCameras[i].enabled = vrMode && _initialVrCameraStates[i];
        }

        // OVRManager remains enabled so switching from desktop to VR during a
        // development session does not require restarting the XR runtime.
        if (_ovrManager != null && vrMode)
        {
            _ovrManager.enabled = true;
        }

        Debug.Log($"Experiment locomotion mode: {controlMode}", this);
    }

    public void ResetToSpawn()
    {
        var wasEnabled = _characterController.enabled;
        _characterController.enabled = false;
        transform.SetPositionAndRotation(_spawnPosition, _spawnRotation);
        _characterController.enabled = wasEnabled;

        _horizontalVelocity = Vector3.zero;
        _verticalVelocity = 0f;
        _desktopPitch = 0f;
        if (_desktopCamera != null)
        {
            _desktopCamera.transform.localRotation = Quaternion.identity;
        }
    }

    private Vector3 ReadDesktopMovement()
    {
        var input = Vector2.zero;
        var gamepad = Gamepad.current;
        if (gamepad != null)
        {
            input = gamepad.leftStick.ReadValue();
        }

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            var keyboardInput = new Vector2(
                (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f),
                (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f));
            if (keyboardInput.sqrMagnitude > input.sqrMagnitude)
            {
                input = keyboardInput;
            }
        }

        input = ApplyDeadZone(input, stickDeadZone);
        var direction = transform.forward * input.y + transform.right * input.x;
        direction.y = 0f;
        return Vector3.ClampMagnitude(direction, 1f);
    }

    private Vector3 ReadVrControllerMovement()
    {
        var leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (!leftController.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out var input))
        {
            input = Vector2.zero;
        }

        input = ApplyDeadZone(input, stickDeadZone);
        var forward = _cameraRig != null && _cameraRig.centerEyeAnchor != null
            ? _cameraRig.centerEyeAnchor.forward
            : transform.forward;
        forward.y = 0f;
        forward.Normalize();
        var right = Vector3.Cross(Vector3.up, forward);
        return Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f);
    }

    private void UpdateDesktopView()
    {
        var look = Vector2.zero;
        var gamepad = Gamepad.current;
        if (gamepad != null)
        {
            look = ApplyDeadZone(gamepad.rightStick.ReadValue(), stickDeadZone) *
                   (gamepadLookSpeed * Time.deltaTime);
        }

        var mouse = Mouse.current;
        if (mouse != null && mouse.rightButton.isPressed)
        {
            look += mouse.delta.ReadValue() * mouseLookScale;
        }

        transform.Rotate(0f, look.x, 0f, Space.World);
        _desktopPitch = Mathf.Clamp(_desktopPitch - look.y, -maximumPitch, maximumPitch);
        if (_desktopCamera != null)
        {
            _desktopCamera.transform.localRotation = Quaternion.Euler(_desktopPitch, 0f, 0f);
        }
    }

    private void UpdateCharacterControllerShape()
    {
        if (_characterController == null) return;

        var vrMode = controlMode == ExperimentControlMode.VrController;
        if (vrMode && _cameraRig != null && _cameraRig.centerEyeAnchor != null)
        {
            var localHead = transform.InverseTransformPoint(_cameraRig.centerEyeAnchor.position);
            var height = Mathf.Clamp(localHead.y, 1f, 2.2f);
            _characterController.height = height;
            _characterController.center = new Vector3(localHead.x, height * 0.5f, localHead.z);
        }
        else
        {
            _characterController.height = 1.7f;
            _characterController.center = new Vector3(0f, 0.85f, 0f);
        }
    }

    private void ApplyMovement(Vector3 horizontalVelocity)
    {
        if (_characterController == null || !_characterController.enabled) return;

        if (_characterController.isGrounded && _verticalVelocity < 0f)
        {
            _verticalVelocity = -1.5f;
        }
        else
        {
            _verticalVelocity += gravity * Time.deltaTime;
        }

        var frameMotion = horizontalVelocity * Time.deltaTime;
        frameMotion.y = _verticalVelocity * Time.deltaTime;
        _characterController.Move(frameMotion);
    }

    private void HandleResetAndPauseInput()
    {
        var keyboard = Keyboard.current;
        var gamepad = Gamepad.current;

        if ((keyboard != null && keyboard.rKey.wasPressedThisFrame) ||
            (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame))
        {
            ResetToSpawn();
        }

        if ((keyboard != null && keyboard.pKey.wasPressedThisFrame) ||
            (gamepad != null && gamepad.startButton.wasPressedThisFrame))
        {
            _paused = !_paused;
            _horizontalVelocity = Vector3.zero;
        }
    }

    private void HandleDevelopmentShortcuts()
    {
        if (!allowDevelopmentModeShortcuts || Keyboard.current == null) return;

        if (Keyboard.current.f1Key.wasPressedThisFrame)
            SetControlMode(ExperimentControlMode.DesktopGamepad);
        if (Keyboard.current.f2Key.wasPressedThisFrame)
            SetControlMode(ExperimentControlMode.VrController);
        if (Keyboard.current.f4Key.wasPressedThisFrame)
            SetControlMode(ExperimentControlMode.Disabled);
    }

    private static Vector2 ApplyDeadZone(Vector2 value, float deadZone)
    {
        var magnitude = value.magnitude;
        if (magnitude <= deadZone) return Vector2.zero;
        var scaledMagnitude = Mathf.InverseLerp(deadZone, 1f, Mathf.Min(1f, magnitude));
        return value.normalized * scaledMagnitude;
    }

    private void OnGUI()
    {
        if (!showDesktopHelp || controlMode != ExperimentControlMode.DesktopGamepad) return;

        const string message =
            "Desktop locomotion test\n" +
            "Move: Xbox left stick / WASD\n" +
            "Look: Xbox right stick / hold right mouse\n" +
            "Reset: A / R    Pause: Menu / P\n" +
            "Modes: F1 Desktop, F2 VR stick, F4 Disabled";
        GUI.Box(new Rect(16f, 16f, 470f, 112f), message);
    }
}
