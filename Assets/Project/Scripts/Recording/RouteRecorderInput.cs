using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace DM2350.Recording
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RouteRecorder))]
    public sealed class RouteRecorderInput : MonoBehaviour
    {
        public string keyboardBinding = "<Keyboard>/g";
        public string gamepadBinding = "<Gamepad>/buttonWest";
        public bool enableQuestLeftX = true;
        private InputAction action;
        private RouteRecorder recorder;
        private bool requested, previousXrPressed;
        private double lastToggle = -1;
        private void OnEnable()
        {
            recorder = GetComponent<RouteRecorder>();
            action = new InputAction("Toggle route recording", InputActionType.Button);
            if (!string.IsNullOrWhiteSpace(keyboardBinding)) action.AddBinding(keyboardBinding);
            if (!string.IsNullOrWhiteSpace(gamepadBinding)) action.AddBinding(gamepadBinding);
            action.performed += OnPerformed;
            action.Enable();
        }
        private void OnPerformed(InputAction.CallbackContext context) => requested = true;
        private void Update()
        {
            var xrPressed = false;
            if (enableQuestLeftX)
            {
                var left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
                left.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out xrPressed);
            }
            requested |= xrPressed && !previousXrPressed;
            previousXrPressed = xrPressed;
            if (!requested) return;
            requested = false;
            if (Time.realtimeSinceStartupAsDouble - lastToggle < 0.25) return;
            lastToggle = Time.realtimeSinceStartupAsDouble;
            recorder.ToggleRecording();
        }
        private void OnDisable()
        {
            action?.Disable();
            action?.Dispose();
            action = null;
            requested = previousXrPressed = false;
        }
    }
}
