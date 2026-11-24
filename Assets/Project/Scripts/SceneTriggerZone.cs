using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SceneTriggerZone : MonoBehaviour
{
    [Tooltip("Tag assigned to your VR Player / XR Origin collider.")]
    [SerializeField] private string playerTag = "Player";

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
            SceneBucketManager.Instance.LoadNextRandomScene();
        }
    }
}
