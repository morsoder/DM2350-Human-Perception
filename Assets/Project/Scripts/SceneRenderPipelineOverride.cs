using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Uses a scene-specific pipeline during Play, then restores the previous quality override.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-1000)]
public sealed class SceneRenderPipelineOverride : MonoBehaviour
{
    [SerializeField] private RenderPipelineAsset pipeline;
    private RenderPipelineAsset _previousPipeline;
    private bool _applied;

    private void OnEnable()
    {
        if (pipeline == null) return;
        _previousPipeline = QualitySettings.renderPipeline;
        QualitySettings.renderPipeline = pipeline;
        _applied = true;
    }

    private void OnDisable()
    {
        if (_applied && QualitySettings.renderPipeline == pipeline)
            QualitySettings.renderPipeline = _previousPipeline;
        _applied = false;
    }
}
