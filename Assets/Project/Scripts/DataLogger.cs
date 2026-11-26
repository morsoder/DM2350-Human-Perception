using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DataLogger : MonoBehaviour
{
    public static DataLogger Instance { get; private set; }

    private string _csvFilePath;
    private string _participantId;
    private string _currentSceneName;
    private float _sceneStartTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Creates a unique CSV file per session on the Meta Quest
        _participantId = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        _csvFilePath = Path.Combine(Application.persistentDataPath, $"participant_{_participantId}.csv");

        File.WriteAllText(_csvFilePath, "ParticipantID,SceneName,Direction,DurationSeconds\n", Encoding.UTF8);
        Debug.Log($"[DataLogger] Saving data to: {_csvFilePath}");
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _currentSceneName = scene.name;
        _sceneStartTime = Time.unscaledTime;
    }

    public void LogChoice(string direction)
    {
        float duration = Time.unscaledTime - _sceneStartTime;
        string line = $"{_participantId},{_currentSceneName},{direction},{duration:F2},{DateTime.Now:HH:mm:ss}\n";

        File.AppendAllText(_csvFilePath, line, Encoding.UTF8);
        Debug.Log($"[DataLogger] Logged: {line.Trim()}");
    }
}