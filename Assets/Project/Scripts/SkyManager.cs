using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Treats every direct child as a sky. Only one is visible at a time.
/// </summary>
public class SkyManager : MonoBehaviour
{
    [Tooltip("Optional skybox materials. Leave empty to use the original child sky meshes.")]
    [SerializeField] private Material[] skyboxMaterials = new Material[0];
    private readonly List<GameObject> _skies = new List<GameObject>();
    private int _currentIndex;

    private bool UsesSkyboxes => skyboxMaterials != null && skyboxMaterials.Length > 0;
    private int Count => UsesSkyboxes ? skyboxMaterials.Length : _skies.Count;
    private int CurrentIndex => _currentIndex;

    private void Awake()
    {
        if (UsesSkyboxes)
        {
            foreach (Transform child in transform) child.gameObject.SetActive(false);
            Show(0);
            return;
        }

        foreach (Transform child in transform)
        {
            _skies.Add(child.gameObject);
        }

        if (_skies.Count == 0)
        {
            Debug.LogWarning("SkyManager has no child objects to use as skies.", this);
            return;
        }

        Show(0);
    }

    /// <summary>Shows the next sky and hides the current one. Wraps around after the last.</summary>
    public void ShowNext()
    {
        if (Count == 0) return;
        Show((_currentIndex + 1) % Count);
    }

    /// <summary>Shows the sky at the given index and hides all others.</summary>
    private void Show(int index)
    {
        if (Count == 0) return;
        _currentIndex = ((index % Count) + Count) % Count;

        if (UsesSkyboxes)
        {
            if (skyboxMaterials[_currentIndex] != null)
                RenderSettings.skybox = skyboxMaterials[_currentIndex];
            return;
        }

        for (var i = 0; i < _skies.Count; i++)
        {
            _skies[i].SetActive(i == _currentIndex);
        }
    }
}
