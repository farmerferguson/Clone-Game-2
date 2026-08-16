using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class levelSelector : MonoBehaviour
{
    [SerializeField] private string LevelName;

    public void OpenLevel()
    {
        SceneManager.LoadScene(LevelName);
    }

}
