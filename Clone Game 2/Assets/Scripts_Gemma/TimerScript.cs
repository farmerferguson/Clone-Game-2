using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using System;

public class timerSystem : MonoBehaviour
{
    bool stopwatchActive = false;
    float currentTime;
    public TextMeshProUGUI currentTimeText;

    void Start()
    {
        currentTime = 0f;
    }

    void Update()
    {
        if (!stopwatchActive && Input.GetMouseButtonDown(0))
        {
            StartStopwatch();
        }

        if (stopwatchActive)
        {
            currentTime += Time.deltaTime;
        }

        TimeSpan time = TimeSpan.FromSeconds(currentTime);
        currentTimeText.text = time.ToString(@"mm\:ss");

    
    }

    public void StartStopwatch()
    {
        stopwatchActive = true;
    }

    public void StopStopwatch()
    {
        stopwatchActive = false;
    }

    // for star system
    public float GetTime()
    {
        return currentTime;
    }
}

