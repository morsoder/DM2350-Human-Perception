using System;
using System.Collections.Generic;
using UnityEngine;

public enum StreetLightingCondition
{
    A_AmbientOnly,
    B_StreetlightsOnly,
    C_WindowLightsOnly,
    D_StreetlightsAndWindows,
    E_DaylightWithShops,
    F_LowNightLighting,
    G_BrightNightLighting
}

/// <summary>Switches experimental lights and their visible emission together.</summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class StreetLightingConditions : MonoBehaviour
{
    [Serializable]
    public sealed class EmissionTarget
    {
        public Renderer renderer;
        public int materialIndex;
        [ColorUsage(false, true)] public Color enabledColor = Color.white;
        public bool offInNightPreset;
    }

    [Tooltip("A retains the environment only. B/C enable one artificial light group; D enables both. E uses the scene's daylight shop-light configuration.")]
    [SerializeField] private StreetLightingCondition condition = StreetLightingCondition.D_StreetlightsAndWindows;
    [SerializeField] private Light[] streetlights = Array.Empty<Light>();
    [SerializeField] private Light[] windowLights = Array.Empty<Light>();
    [SerializeField] private EmissionTarget[] streetEmission = Array.Empty<EmissionTarget>();
    [SerializeField] private EmissionTarget[] windowEmission = Array.Empty<EmissionTarget>();
    [SerializeField] private ReflectionProbe[] reflectionProbes = Array.Empty<ReflectionProbe>();

    [Tooltip("These lights remain off in the fixed low/bright night preset. Other lighting modes retain their original behaviour.")]
    [SerializeField] private Light[] lightsOffInNightPreset = Array.Empty<Light>();

    public void ConfigureNightPreset(Light[] offLights)
    {
        lightsOffInNightPreset = offLights ?? Array.Empty<Light>();
        if (isActiveAndEnabled) Apply();
    }

    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    private readonly Dictionary<Light, bool> originalLights = new Dictionary<Light, bool>();
    private readonly Dictionary<EmissionTarget, MaterialPropertyBlock> originalBlocks = new Dictionary<EmissionTarget, MaterialPropertyBlock>();
    private bool refreshPending;

    public StreetLightingCondition Condition => condition;

    public void Configure(Light[] street, Light[] windows, EmissionTarget[] streetMaterials,
        EmissionTarget[] windowMaterials, ReflectionProbe[] probes)
    {
        Restore();
        streetlights = street;
        windowLights = windows;
        streetEmission = streetMaterials;
        windowEmission = windowMaterials;
        reflectionProbes = probes;
        Capture();
        Apply();
    }

    public void SetCondition(StreetLightingCondition value)
    {
        condition = value;
        if (isActiveAndEnabled) Apply();
    }

    private void OnEnable()
    {
        Capture();
        Apply();
    }

    private void OnValidate() => refreshPending = true;

    private void Update()
    {
        if (!refreshPending) return;
        refreshPending = false;
        Apply();
    }

    private void OnDisable() => Restore();

    private void Capture()
    {
        CaptureLights(streetlights);
        CaptureLights(windowLights);
        CaptureMaterials(streetEmission);
        CaptureMaterials(windowEmission);
    }

    private void CaptureLights(Light[] lights)
    {
        foreach (var light in lights)
            if (light != null && !originalLights.ContainsKey(light)) originalLights.Add(light, light.enabled);
    }

    private void CaptureMaterials(EmissionTarget[] targets)
    {
        foreach (var target in targets)
        {
            if (target == null || target.renderer == null || originalBlocks.ContainsKey(target)) continue;
            var block = new MaterialPropertyBlock();
            target.renderer.GetPropertyBlock(block, target.materialIndex);
            originalBlocks.Add(target, block);
        }
    }

    private void Apply()
    {
        Capture();
        bool nightPreset = condition == StreetLightingCondition.F_LowNightLighting || condition == StreetLightingCondition.G_BrightNightLighting;
        bool streetOn = nightPreset || condition == StreetLightingCondition.B_StreetlightsOnly || condition == StreetLightingCondition.D_StreetlightsAndWindows;
        bool windowsOn = nightPreset || condition == StreetLightingCondition.C_WindowLightsOnly || condition == StreetLightingCondition.D_StreetlightsAndWindows || condition == StreetLightingCondition.E_DaylightWithShops;
        foreach (var light in streetlights) if (light != null) light.enabled = streetOn;
        foreach (var light in windowLights) if (light != null) light.enabled = windowsOn;
        if (nightPreset)
            foreach (var light in lightsOffInNightPreset) if (light != null) light.enabled = false;
        SetEmission(streetEmission, streetOn, nightPreset);
        SetEmission(windowEmission, windowsOn, nightPreset);
        foreach (var probe in reflectionProbes)
            if (probe != null && probe.isActiveAndEnabled && probe.mode == UnityEngine.Rendering.ReflectionProbeMode.Realtime)
                probe.RenderProbe();
    }

    private static void SetEmission(EmissionTarget[] targets, bool enabled, bool nightPreset)
    {
        foreach (var target in targets)
        {
            if (target == null || target.renderer == null) continue;
            var block = new MaterialPropertyBlock();
            target.renderer.GetPropertyBlock(block, target.materialIndex);
            block.SetColor(EmissionColor, enabled && !(nightPreset && target.offInNightPreset) ? target.enabledColor : Color.black);
            target.renderer.SetPropertyBlock(block, target.materialIndex);
        }
    }

    private void Restore()
    {
        foreach (var pair in originalLights) if (pair.Key != null) pair.Key.enabled = pair.Value;
        foreach (var pair in originalBlocks)
            if (pair.Key.renderer != null)
                pair.Key.renderer.SetPropertyBlock(pair.Value.isEmpty ? null : pair.Value, pair.Key.materialIndex);
        originalLights.Clear();
        originalBlocks.Clear();
    }
}
