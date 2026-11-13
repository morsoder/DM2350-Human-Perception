using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Reproducible intermittent outages, shared by illumination and the bulb mesh.
/// Only the assigned bulb material is cloned; other lamps keep their materials.
/// </summary>
[DisallowMultipleComponent]
public sealed class FaultyStreetLight : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private Light targetLight;
    [SerializeField] private Renderer emissiveRenderer;
    [SerializeField, Min(0)] private int emissiveMaterialIndex;

    [Header("Timing (seconds)")]
    [Tooltip("Time at full brightness between fault bursts.")]
    [SerializeField] private Vector2 normalDuration = new Vector2(4f, 8f);
    [Tooltip("Number of dim/off pulses in each burst, inclusive.")]
    [SerializeField] private Vector2Int pulsesPerBurst = new Vector2Int(1, 2);
    [SerializeField] private Vector2 outageDuration = new Vector2(0.18f, 0.4f);
    [SerializeField] private Vector2 recoveryDuration = new Vector2(0.25f, 0.5f);
    [Tooltip("Remaining light during a fault: 0 is completely off, 1 is normal.")]
    [SerializeField, Range(0f, 1f)] private float minimumBrightness = 0.08f;

    [Header("Repeatable sequence")]
    [Tooltip("The same seed and settings reproduce the same 64-burst loop.")]
    [SerializeField] private int randomSeed = 235001;
    [Tooltip("Offset into the sequence, so nearby lamps can start out of step.")]
    [SerializeField, Min(0f)] private float startOffsetSeconds;

    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    private const int BurstsPerLoop = 64;
    private readonly List<Segment> _segments = new List<Segment>();
    private double _loopDuration;
    private double _elapsed;
    private float _originalIntensity;
    private bool _originalLightEnabled;
    private Color _originalEmission;
    private Material[] _originalMaterials;
    private Material _bulbMaterial;
    private bool _initialized;

    public float NormalizedBrightness { get; private set; } = 1f;

    private struct Segment
    {
        public double End;
        public float Brightness;
    }

    private void OnEnable()
    {
        if (targetLight == null || emissiveRenderer == null)
        {
            Debug.LogWarning("FaultyStreetLight requires a light and a bulb renderer.", this);
            return;
        }

        var materials = emissiveRenderer.sharedMaterials;
        if (emissiveMaterialIndex < 0 || emissiveMaterialIndex >= materials.Length ||
            materials[emissiveMaterialIndex] == null ||
            !materials[emissiveMaterialIndex].HasProperty(EmissionColor))
        {
            Debug.LogWarning("FaultyStreetLight requires a material with _EmissionColor.", this);
            return;
        }

        _originalIntensity = targetLight.intensity;
        _originalLightEnabled = targetLight.enabled;
        _originalMaterials = (Material[])materials.Clone();
        _bulbMaterial = new Material(materials[emissiveMaterialIndex]);
        _bulbMaterial.name += " (Faulty light instance)";
        _bulbMaterial.hideFlags = HideFlags.DontSave;
        _originalEmission = _bulbMaterial.GetColor(EmissionColor);
        _bulbMaterial.EnableKeyword("_EMISSION");
        materials[emissiveMaterialIndex] = _bulbMaterial;
        emissiveRenderer.sharedMaterials = materials;
        _initialized = true;
        RestartSequence();
    }

    /// <summary>Restarts this lamp's repeatable timeline, including its configured offset.</summary>
    public void RestartSequence()
    {
        BuildSequence();
        _elapsed = 0d;
        if (_initialized) ApplyBrightness(EvaluateBrightness(startOffsetSeconds));
    }

    private void Update()
    {
        if (!_initialized) return;
        _elapsed += Time.deltaTime;
        ApplyBrightness(EvaluateBrightness(_elapsed + startOffsetSeconds));
    }

    private void BuildSequence()
    {
        _segments.Clear();
        _loopDuration = 0d;
        var random = new System.Random(randomSeed);
        var pulseMin = Mathf.Clamp(pulsesPerBurst.x, 1, 12);
        var pulseMax = Mathf.Clamp(pulsesPerBurst.y, pulseMin, 12);

        for (var burst = 0; burst < BurstsPerLoop; burst++)
        {
            AddSegment(SampleDuration(random, normalDuration), 1f);
            var pulses = random.Next(pulseMin, pulseMax + 1);
            for (var pulse = 0; pulse < pulses; pulse++)
            {
                AddSegment(SampleDuration(random, outageDuration), Mathf.Clamp01(minimumBrightness));
                if (pulse + 1 < pulses)
                    AddSegment(SampleDuration(random, recoveryDuration), 1f);
            }
        }
    }

    private static double SampleDuration(System.Random random, Vector2 range)
    {
        var min = Mathf.Max(0.05f, Mathf.Min(range.x, range.y));
        var max = Mathf.Max(min, Mathf.Max(range.x, range.y));
        return min + random.NextDouble() * (max - min);
    }

    private void AddSegment(double duration, float brightness)
    {
        _loopDuration += duration;
        _segments.Add(new Segment { End = _loopDuration, Brightness = brightness });
    }

    private float EvaluateBrightness(double elapsed)
    {
        if (_segments.Count == 0) return 1f;
        var time = Math.Max(0d, elapsed) % _loopDuration;
        var low = 0;
        var high = _segments.Count - 1;
        while (low < high)
        {
            var middle = (low + high) / 2;
            if (time < _segments[middle].End) high = middle;
            else low = middle + 1;
        }
        return _segments[low].Brightness;
    }

    private void ApplyBrightness(float brightness)
    {
        NormalizedBrightness = brightness;
        if (targetLight != null)
        {
            targetLight.intensity = _originalIntensity * brightness;
            targetLight.enabled = _originalLightEnabled && brightness > 0f;
        }
        if (_bulbMaterial != null)
            _bulbMaterial.SetColor(EmissionColor, _originalEmission * brightness);
    }

    private void OnDisable()
    {
        if (!_initialized) return;
        if (targetLight != null)
        {
            targetLight.intensity = _originalIntensity;
            targetLight.enabled = _originalLightEnabled;
        }
        if (emissiveRenderer != null) emissiveRenderer.sharedMaterials = _originalMaterials;
        if (_bulbMaterial != null) Destroy(_bulbMaterial);
        _bulbMaterial = null;
        _originalMaterials = null;
        _initialized = false;
        NormalizedBrightness = 1f;
    }
}
