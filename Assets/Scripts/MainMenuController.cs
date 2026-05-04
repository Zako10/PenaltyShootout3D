using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MainMenuController : MonoBehaviour
{
    [SerializeField] private string easyLevelSceneName = "EasyPenaltyShootout";

    public void LoadEasyLevel()
    {
        SceneManager.LoadScene(easyLevelSceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
