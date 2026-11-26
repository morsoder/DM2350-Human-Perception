using DM2350.Recording;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SceneTriggerZone : MonoBehaviour
{
    public enum WalkDirection
    {
        Left,
        Right,
        Start
    }

    [Tooltip("Tag assigned to your VR Player / XR Origin collider.")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("Which direction this trigger zone represents.")]
    [SerializeField] private WalkDirection direction = WalkDirection.Left;

    private bool hasTriggered;

    private void Awake()
    {
        // A kinematic body ensures the zone receives events from the study CharacterController.
        var zone = GetComponent<Collider>();
        if (zone.attachedRigidbody == null)
        {
            var body = gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
        }
    }

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        var manager = SceneBucketManager.Instance;
        if (hasTriggered || manager == null || manager.IsLoading) return;

        var study = other.GetComponentInParent<GroupStudyPlayerSetup>();
        if (!(other is CharacterController && study != null) &&
            !other.CompareTag(playerTag) && !other.transform.root.CompareTag(playerTag))
            return;

        var recorder = study == null ? null : study.GetComponentInChildren<RouteRecorder>(true);
        if (recorder != null && recorder.IsRecording && !recorder.StopRecording("scene_trigger"))
        {
            Debug.LogError("The route could not be saved. Scene transition cancelled.", this);
            return;
        }

        if (!manager.TryLoadNextRandomScene(recorder)) return;

        hasTriggered = true;
        DataLogger.Instance?.LogChoice(direction.ToString());
    }
}