using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;

// Handles buttons in game over/victory scenes
public class GameOverButtons : MonoBehaviour
{
    public void RetryGame()
    {
        string lastMap = GameManager.Instance.CurrentMap;
        if (!string.IsNullOrEmpty(lastMap))
        {
            SceneManager.LoadScene(lastMap);
        }
        else
        {
            Debug.LogError("No map found to reload!");
        }
    }

    public void MainMenu()
    {
        SceneManager.LoadScene("Menu");
    }
}
