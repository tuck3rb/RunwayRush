using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject playPanel;
    [SerializeField] private GameObject howToPanel;

    // Fields
    private string selectedMap;
    private float gameDuration = -1f;  // -1 for infinite
    private TrafficLevel trafficLevel = TrafficLevel.Medium;
    private bool emergenciesEnabled = false;

    private void Start()
    {
        creditsPanel.SetActive(false);
    }

    public void HowToButton()
    {
        howToPanel.SetActive(true);
    }

    public void PlayButton()
    {
        playPanel.SetActive(true);
    }

    public void CreditsButton()
    {
        creditsPanel.SetActive(true);
    }

    public void BackButton()
    {
        creditsPanel.SetActive(false);
        playPanel.SetActive(false);
        howToPanel.SetActive(false);
    }

    public void SelectMap(string mapName)
    {
        selectedMap = mapName;
    }

    public void SetGameDuration(float minutes)
    {
        gameDuration = minutes * 60;
    }

    public void SetTrafficLevel(int level)
    {
        trafficLevel = (TrafficLevel)level;
    }

    public void ToggleEmergencies(bool enabled)
    {
        emergenciesEnabled = enabled;
    }

    public void StartGame()
    {
        if (!string.IsNullOrEmpty(selectedMap))
        {
            GameManager.Instance.SetGameSettings(selectedMap, gameDuration, trafficLevel, emergenciesEnabled);
            SceneManager.LoadScene(selectedMap);
        }
    }
}
