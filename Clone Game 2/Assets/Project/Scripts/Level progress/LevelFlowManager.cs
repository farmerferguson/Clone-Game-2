using UnityEngine;
using UnityEngine.SceneManagement;
using PipeHack.Grid;

namespace PipeHack.Flow
{
    /// <summary>
    /// Advances Scene 1 -> 2 -> 3 only when the current scene's GridManager
    /// reports a win. Deliberately knows nothing about grid size or level
    /// rules - those stay owned by the grid root prefab in each scene, this
    /// just reacts to the win event and loads the next scene by name.
    /// </summary>
    public class LevelFlowManager : MonoBehaviour
    {
        [Tooltip("GridManager for this scene.")]
        [SerializeField] private GridManager gridManager;

        [Tooltip("Scene to load when this level is won. Leave blank if this is the final level.")]
        [SerializeField] private string nextSceneName;

        [Tooltip("Delay after winning before loading the next scene, so the fill animation/feedback has time to finish.")]
        [SerializeField] private float loadDelay = 1.5f;

        private void OnEnable()
        {
            if (gridManager != null)
                gridManager.OnLevelWon += HandleLevelWon;
        }

        private void OnDisable()
        {
            if (gridManager != null)
                gridManager.OnLevelWon -= HandleLevelWon;
        }

        private void HandleLevelWon()
        {
            if (string.IsNullOrEmpty(nextSceneName))
            {
                Debug.Log("[LevelFlowManager] Final level won - no next scene configured.");
                return;
            }

            Invoke(nameof(LoadNextScene), loadDelay);
        }

        private void LoadNextScene()
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
