using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Collections;

// Keeps time for various reasons
public class Timer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    private float currentTime = 0f;

    void Update()
    {
        if (Time.timeScale > 0) // for paused
        {
            currentTime += Time.deltaTime;
            UpdateTimerDisplay();
        }
    }

    void UpdateTimerDisplay()
    {
        float minutes = Mathf.FloorToInt(currentTime / 60);
        float seconds = Mathf.FloorToInt(currentTime % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}
