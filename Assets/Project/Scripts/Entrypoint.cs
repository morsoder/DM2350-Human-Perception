using UnityEngine;

public class Entrypoint : MonoBehaviour
{
    [SerializeField] private SkyManager skyManager;
    [SerializeField] private TriggerPoint[] triggerPoints;
    [SerializeField] private OVRCameraRig cameraRig;

    private int _activeIndex;

    private void Start()
    {
        foreach (var point in triggerPoints)
        {
            point.Initialize(cameraRig.centerEyeAnchor);
            point.Triggered += OnTriggerPointReached;
            point.gameObject.SetActive(false);
        }

        ActivateTriggerPoint(0);
    }

    private void OnDestroy()
    {
        foreach (var point in triggerPoints)
        {
            point.Triggered -= OnTriggerPointReached;
        }
    }

    // We own the state: only the point at _activeIndex is switched on, so it must be the one that fired.
    private void OnTriggerPointReached()
    {
        triggerPoints[_activeIndex].gameObject.SetActive(false);
        skyManager.ShowNext();
        ActivateTriggerPoint(_activeIndex + 1);
    }

    private void ActivateTriggerPoint(int index)
    {
        _activeIndex = index % triggerPoints.Length;
        triggerPoints[_activeIndex].gameObject.SetActive(true);
    }
}
