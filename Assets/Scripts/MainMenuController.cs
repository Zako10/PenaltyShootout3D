using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MainMenuController : MonoBehaviour
{
    [SerializeField] private string easyLevelSceneName = "EasyPenaltyShootout";
    [SerializeField] private string hardLevelSceneName = "HardPenaltyShootout";



    public void LoadEasyLevel()
    {
        PlayerPrefs.SetInt("DifficultyLevel", 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene(easyLevelSceneName);
    }


    public void LoadHardLevel()
    {
        PlayerPrefs.SetInt("DifficultyLevel", 3);
        PlayerPrefs.Save();
        SceneManager.LoadScene(hardLevelSceneName);
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
