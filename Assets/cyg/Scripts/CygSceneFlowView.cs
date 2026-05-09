using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Cyg.UI
{
    [DisallowMultipleComponent]
    public sealed class CygSceneFlowView : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject clearPanel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private bool hidePanelsOnAwake = true;

        [Header("Buttons")]
        [SerializeField] private Button[] restartButtons;
        [SerializeField] private Button[] quitButtons;
        [SerializeField] private bool autoFindReferences = true;

        private void Awake()
        {
            if (autoFindReferences)
                FindMissingReferences();

            BindButtons();

            if (hidePanelsOnAwake)
                HideAllPanels();
        }

        private void OnEnable()
        {
            GameEvents.OnCombatEnded += HandleCombatEnded;
        }

        private void OnDisable()
        {
            GameEvents.OnCombatEnded -= HandleCombatEnded;
        }

        public void RestartScene()
        {
            Time.timeScale = 1f;
            Scene currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.buildIndex);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void HideAllPanels()
        {
            SetPanelActive(clearPanel, false);
            SetPanelActive(gameOverPanel, false);
        }

        private void HandleCombatEnded(bool win)
        {
            SetPanelActive(clearPanel, win);
            SetPanelActive(gameOverPanel, !win);
        }

        private void BindButtons()
        {
            if (restartButtons != null)
            {
                for (int i = 0; i < restartButtons.Length; i++)
                {
                    if (restartButtons[i] == null)
                        continue;

                    restartButtons[i].onClick.RemoveListener(RestartScene);
                    restartButtons[i].onClick.AddListener(RestartScene);
                }
            }

            if (quitButtons != null)
            {
                for (int i = 0; i < quitButtons.Length; i++)
                {
                    if (quitButtons[i] == null)
                        continue;

                    quitButtons[i].onClick.RemoveListener(QuitGame);
                    quitButtons[i].onClick.AddListener(QuitGame);
                }
            }
        }

        private void FindMissingReferences()
        {
            if (clearPanel == null)
                clearPanel = FindChildByNameContains("Clear");

            if (gameOverPanel == null)
                gameOverPanel = FindChildByNameContains("GameOver");

            if (restartButtons == null || restartButtons.Length == 0)
                restartButtons = FindButtonsByName("Restart");

            if (quitButtons == null || quitButtons.Length == 0)
                quitButtons = FindButtonsByName("Quit");
        }

        private GameObject FindChildByNameContains(string token)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] == transform)
                    continue;

                if (children[i].name.Contains(token))
                    return children[i].gameObject;
            }

            return null;
        }

        private Button[] FindButtonsByName(string buttonName)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            int count = 0;

            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i].name == buttonName)
                    count++;
            }

            Button[] result = new Button[count];
            int resultIndex = 0;
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i].name == buttonName)
                    result[resultIndex++] = buttons[i];
            }

            return result;
        }

        private static void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
                panel.SetActive(active);
        }
    }
}
