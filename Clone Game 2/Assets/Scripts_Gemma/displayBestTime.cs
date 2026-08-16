using UnityEngine;
using TMPro;
using System;

public class displayBestTime : MonoBehaviour
{
    [SerializeField] private string levelName;
    [SerializeField] private TextMeshProUGUI timeText;

    void Start()
    {
        DisplayBestTime();
    }

    public void DisplayBestTime()
    {
        string saveKey = "BestTime_" + levelName;

        if (PlayerPrefs.HasKey(saveKey))
        {
            float bestTime = PlayerPrefs.GetFloat(saveKey);
            TimeSpan time = TimeSpan.FromSeconds(bestTime);
            timeText.text = time.ToString(@"mm\:ss");
        }
        else
        {
            timeText.text = "--:--";
        }
    }
}
