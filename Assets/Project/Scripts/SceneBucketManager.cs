using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SceneBucketManager : MonoBehaviour
{
    public static SceneBucketManager Instance { get; private set; }

    [Header("Scene Pool")]
#if UNITY_EDITOR
    [Tooltip("Drag a folder from the Project window (e.g. Assets/Project/Scenes/Group1).")]
    [SerializeField] private DefaultAsset sceneFolder;
#endif

    [Tooltip("Automatically populated with enabled scenes from the Build List inside the chosen folder.")]
    [SerializeField] private List<string> sceneNames = new List<string>();

    [Tooltip("Scene to load once the bucket is empty (leave blank to stop or reset).")]
    [SerializeField] private string finalSceneName = "";

    public const string StudyStartPath = "Assets/Project/Scenes/SwitchingTestScenes/StartTestScene.unity";
    private const string StudyScenesRoot = "Assets/Project/Scenes/";

    private List<string> remainingScenes;
    private string studyFolder;
    private string participantId;
    private bool returnToStudyStart;
    public bool IsLoading { get; private set; }

    public static bool TryStartStudy(int group)
    {
        if (group < 1 || group > 3) return false;
        if (Instance == null)
            new GameObject(nameof(SceneBucketManager)).AddComponent<SceneBucketManager>();
        if (Instance.IsLoading) return false;

        Instance.studyFolder = StudyScenesRoot + "Group" + group;
        Instance.ResetBucket(SceneManager.GetActiveScene());
        if (Instance.remainingScenes.Count == 0)
        {
            Instance.studyFolder = null;
            return false;
        }

        Instance.returnToStudyStart = true;
        return Instance.TryLoadNextRandomScene(null);
    }

    public static void PrepareStudyScene(Scene scene, DM2350.Recording.RouteRecorder recorder)
    {
        var separator = scene.path.LastIndexOf('/');
        if (separator < 0) return;
        var folder = scene.path.Substring(0, separator);
        if (Instance == null)
            new GameObject(nameof(SceneBucketManager)).AddComponent<SceneBucketManager>();

        var manager = Instance;
        if (manager.studyFolder != folder)
        {
            manager.studyFolder = folder;
            manager.ResetBucket(scene);
        }

        if (recorder != null && manager.participantId != null)
            recorder.participantId = manager.participantId;
    }

    private void Awake()
    {
        // Singleton pattern to ensure only one manager persists across scenes
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        ResetBucket();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        IsLoading = false;
        if (scene.path != StudyStartPath) return;

        studyFolder = null;
        returnToStudyStart = false;
        ResetBucket(scene);
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Instance = null;
    }

    public void ResetBucket() => ResetBucket(SceneManager.GetActiveScene());

    private void ResetBucket(Scene current)
    {
        participantId = null;
        if (studyFolder == null)
        {
            remainingScenes = new List<string>(sceneNames);
            remainingScenes.Remove(current.name);
            remainingScenes.Remove(current.path);
            return;
        }

        remainingScenes = new List<string>();
        var folderPrefix = studyFolder + "/";
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            var path = SceneUtility.GetScenePathByBuildIndex(i);
            if (path.LastIndexOf('/') == studyFolder.Length &&
                path.StartsWith(folderPrefix, System.StringComparison.Ordinal) && path != current.path)
                remainingScenes.Add(path);
        }
    }

    public void LoadNextRandomScene() => TryLoadNextRandomScene(null);

    public bool TryLoadNextRandomScene(DM2350.Recording.RouteRecorder recorder)
    {
        if (IsLoading) return false;
        if (recorder != null) participantId = recorder.participantId;
        if (remainingScenes.Count == 0)
        {
            if (returnToStudyStart)
            {
                int startIndex = SceneUtility.GetBuildIndexByScenePath(StudyStartPath);
                if (startIndex < 0)
                {
                    Debug.LogError("StartTestScene is missing from the build.");
                    return false;
                }
                IsLoading = SceneManager.LoadSceneAsync(startIndex) != null;
                return IsLoading;
            }

            if (studyFolder != null || string.IsNullOrEmpty(finalSceneName))
            {
                Debug.Log("Scene bucket is empty!");
                return false;
            }
            IsLoading = SceneManager.LoadSceneAsync(finalSceneName) != null;
            return IsLoading;
        }

        int randomIndex = Random.Range(0, remainingScenes.Count);
        string nextScene = remainingScenes[randomIndex];
        int buildIndex = SceneUtility.GetBuildIndexByScenePath(nextScene);
        IsLoading = (buildIndex >= 0 ? SceneManager.LoadSceneAsync(buildIndex) : SceneManager.LoadSceneAsync(nextScene)) != null;
        if (!IsLoading) return false;
        remainingScenes.RemoveAt(randomIndex);
        Debug.Log("Number of scenes in the bucket: " + remainingScenes.Count);
        return true;
    }
#if UNITY_EDITOR
    private void OnValidate()
    {
        FilterScenesFromBuildList();
    }

    [ContextMenu("Refresh Scenes From Folder")]
    public void FilterScenesFromBuildList()
    {
        if (sceneFolder == null)
            return;

        string folderPath = AssetDatabase.GetAssetPath(sceneFolder);
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            Debug.LogWarning($"'{sceneFolder.name}' is not a valid folder.", this);
            return;
        }

        string folderPrefix = folderPath.EndsWith("/") ? folderPath : folderPath + "/";
        sceneNames.Clear();

        foreach (var buildScene in EditorBuildSettings.scenes)
        {
            if (buildScene.enabled && buildScene.path.StartsWith(folderPrefix))
            {
                sceneNames.Add(buildScene.path);
            }
        }
    }
#endif
}