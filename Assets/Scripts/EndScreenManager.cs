using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
using TMPro;

// Determines text on game over screen
public class EndScreenManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI reasonText;

    void Start()
    {
        float finalTime = GameManager.Instance.gameTime;
        float minutes = Mathf.FloorToInt(finalTime / 60);
        float seconds = Mathf.FloorToInt(finalTime % 60);
        timeText.text = $"Final Time: {minutes:00}:{seconds:00}";

        switch (GameManager.Instance.endReason)
        {
            case GameEndReason.Collision:
                reasonText.text = "Aircraft Collision";
                break;
            case GameEndReason.EmergencyFailed:
                reasonText.text = "Emergency Situation Botched";
                break;
            case GameEndReason.TimerComplete:
                reasonText.text = "Mission Complete!";
                break;
        }
    }
}
