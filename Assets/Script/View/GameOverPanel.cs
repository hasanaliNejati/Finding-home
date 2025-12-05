using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Script.View
{
    public class GameOverPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI gameOverText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button quitButton;

        private GamePlayManager gamePlayManager;

        private void Start()
        {
            gamePlayManager = GamePlayManager.Instance;
            
            if (gamePlayManager != null)
            {
                gamePlayManager.OnGameOver += ShowPanel;
            }

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(RestartGame);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(QuitGame);
            }

            // Initially hide the panel
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        private void ShowPanel()
        {
            if (panel != null)
            {
                panel.SetActive(true);
            }

            if (gameOverText != null)
            {
                gameOverText.text = "Game Over!\nAll penguins have died.";
            }
        }

        private void RestartGame()
        {
            // Reload the current scene
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
            );
        }

        private void QuitGame()
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        private void OnDestroy()
        {
            if (gamePlayManager != null)
            {
                gamePlayManager.OnGameOver -= ShowPanel;
            }
        }
    }
}




