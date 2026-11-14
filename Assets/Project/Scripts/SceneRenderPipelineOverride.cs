using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Uses a scene-specific pipeline, optionally including the editor preview.</summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[DefaultExecutionOrder(-1000)]
public sealed class SceneRenderPipelineOverride : MonoBehaviour
{
    [SerializeField] private RenderPipelineAsset pipeline;
    [Tooltip("Preview this scene's lighting pipeline before Play. Other scenes keep their own pipeline.")]
    [SerializeField] private bool previewInEditor;
    [SerializeField, HideInInspector] private RenderPipelineAsset editorFallbackPipeline;
    private RenderPipelineAsset previousPipeline;
    private RenderPipelineAsset appliedPipeline;
    private bool applied;
    private bool refreshPending;

    private void OnEnable()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.quitting -= BeforeEditorQuit;
        UnityEditor.EditorApplication.quitting += BeforeEditorQuit;
        UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= Restore;
        UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += Restore;
        UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeChanged;
#endif
        Apply();
    }
    private void OnValidate() => refreshPending = true;

    private void Update()
    {
        if (!refreshPending) return;
        refreshPending = false;
        Restore();
        Apply();
    }

    private void Apply()
    {
        if (applied) return;
        if (pipeline == null || (!Application.isPlaying && !previewInEditor) || !gameObject.scene.IsValid()) return;
        previousPipeline = QualitySettings.renderPipeline;
#if UNITY_EDITOR
        // Editor saves and play-mode reloads can retain the temporary quality override.
        // The authored fallback preserves the project setting across those boundaries.
        if (previousPipeline == pipeline) previousPipeline = editorFallbackPipeline;
#endif
        appliedPipeline = pipeline;
        QualitySettings.renderPipeline = pipeline;
        applied = true;
    }

    private void OnDisable()
    {
        Restore();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.quitting -= BeforeEditorQuit;
        UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= Restore;
        UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeChanged;
#endif
    }

#if UNITY_EDITOR
    private void BeforeEditorQuit()
    {
        Restore();
        UnityEditor.AssetDatabase.SaveAssets();
    }

    private void OnPlayModeChanged(UnityEditor.PlayModeStateChange state)
    {
        if (state == UnityEditor.PlayModeStateChange.ExitingEditMode || state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
            Restore();
        else
            refreshPending = true;
    }
#endif

    private void Restore()
    {
        if (applied && QualitySettings.renderPipeline == appliedPipeline)
            QualitySettings.renderPipeline = previousPipeline;
        applied = false;
    }
}
