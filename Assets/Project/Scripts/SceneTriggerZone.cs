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

    private bool hasTriggered = false;

    private void Reset()
    {
        // Automatically ensure the collider acts as a trigger
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        // Check root or collider tag (VR rigs often nest colliders on the Camera or CharacterController)
        if (other.CompareTag(playerTag) || other.transform.root.CompareTag(playerTag))
        {
            hasTriggered = true;

            DataLogger.Instance?.LogChoice(direction.ToString());
            SceneBucketManager.Instance.LoadNextRandomScene();
        }
    }
}