using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // Fields
    public string CurrentMap { get; private set; }
    public float GameDuration { get; private set; }
    public TrafficLevel TrafficLevel { get; private set; }
    public bool EmergenciesEnabled { get; private set; }
    private string lastScene;
    public float gameTime { get; private set; } = 0f;
    public GameEndReason endReason;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Update()
    {
        if (Time.timeScale > 0) // keep up with final time (for infinite mode mainly)
        {
            gameTime += Time.deltaTime;
        }
    }

    public void GameOver(GameEndReason reason)
    {
        endReason = reason;
        lastScene = SceneManager.GetActiveScene().name;
        StartCoroutine(GameOverSequence());
    }

    public void GoHome()
    {
        SceneManager.LoadScene("Menu");
    }

    private IEnumerator GameOverSequence()
    {
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene("GameOver");
    }

    public string GetLastScene() 
    {
        return lastScene; // Gets previous scene for replay button
    }
    
    public void SetGameSettings(string map, float duration, TrafficLevel traffic, bool emergencies)
    {
        CurrentMap = map;
        GameDuration = duration;
        TrafficLevel = traffic;
        EmergenciesEnabled = emergencies;
    }

    public void Victory()
    {
        endReason = GameEndReason.TimerComplete;
        SceneManager.LoadScene("Victory"); 
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Menu" && scene.name != "GameOver" && scene.name != "Victory")
        {
            gameTime = 0f;
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
