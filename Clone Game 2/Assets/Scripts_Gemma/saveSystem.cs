using UnityEngine;
using TMPro;
using System;

public class saveSystem : MonoBehaviour
{
    [SerializeField] private string levelName;

    // Saves the time only if it is faster than the previous time
    public void SaveTime(float newTime)
    {
        string saveKey = "BestTime_" + levelName;

        // Check if a time has already been saved
        if (PlayerPrefs.HasKey(saveKey))
        {
            float oldTime = PlayerPrefs.GetFloat(saveKey);

            // Only save if the new time is faster
            if (newTime < oldTime)
            {
                PlayerPrefs.SetFloat(saveKey, newTime);
                PlayerPrefs.Save();
            }
        }
        else
        {
            // No previous time, so save this one
            PlayerPrefs.SetFloat(saveKey, newTime);
            PlayerPrefs.Save();
        }
    }

    // Gets the saved best time
    public float GetBestTime()
    {
        string saveKey = "BestTime_" + levelName;

        return PlayerPrefs.GetFloat(saveKey, 0f);
    }
}
