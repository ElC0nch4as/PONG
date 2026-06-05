using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private string playSceneName = "SampleScene";
    [SerializeField] private string classicSceneName = "ClassicEscene";
    [SerializeField] private string twoVsTwoSceneName = "2vs2Scene";

    public void PlayTron()
    {
        PlayerPrefs.SetInt("GAME_MODE", 1);
        SceneManager.LoadScene(playSceneName);
    }

    public void PlayClassic()
    {
        PlayerPrefs.SetInt("GAME_MODE", 2);
        SceneManager.LoadScene(classicSceneName);
    }

    public void Play2vs2()
    {
        PlayerPrefs.SetInt("GAME_MODE", 3);
        SceneManager.LoadScene(3);
    }
}