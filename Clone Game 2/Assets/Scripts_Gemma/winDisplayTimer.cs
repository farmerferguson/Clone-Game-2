using UnityEngine;
using TMPro;
using System;

public class winDisplayTimer : MonoBehaviour
{
    [SerializeField] private timerSystem timer;
    [SerializeField] private TextMeshProUGUI timeText;

    public void ShowTime()
    {
        float finalTime = timer.GetTime();

        TimeSpan time = TimeSpan.FromSeconds(finalTime);

        timeText.text = time.ToString(@"mm\:ss");
    }
}
