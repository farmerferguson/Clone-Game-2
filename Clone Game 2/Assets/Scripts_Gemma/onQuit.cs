using UnityEngine;

public class onQuit : MonoBehaviour
{
    public void Quit()
    {
        Application.Quit();

        Debug.Log ("[OnQuit] Program closed");
    }
}
