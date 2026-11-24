using System.Collections.Generic;
using System.IO;
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

    private List<string> remainingScenes;

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
    }

    public void ResetBucket()
    {
        remainingScenes = new List<string>(sceneNames);

        // Remove the starting scene if it happens to be in the list
        string currentScene = SceneManager.GetActiveScene().name;
        remainingScenes.Remove(currentScene);
    }

    public void LoadNextRandomScene()
    {
        if (remainingScenes.Count == 0)
        {
            if (!string.IsNullOrEmpty(finalSceneName))
            {
                SceneManager.LoadScene(finalSceneName);
            }
            else
            {
                Debug.Log("Scene bucket is empty!");
            }
            return;
        }

        // Pick a random index, grab the name, and remove it from the bucket
        int randomIndex = Random.Range(0, remainingScenes.Count);
        string nextScene = remainingScenes[randomIndex];
        remainingScenes.RemoveAt(randomIndex);

        Debug.Log("Number of scenes in the bucket: " + remainingScenes.Count);
        SceneManager.LoadScene(nextScene);
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
                sceneNames.Add(Path.GetFileNameWithoutExtension(buildScene.path));
            }
        }
    }
#endif
}