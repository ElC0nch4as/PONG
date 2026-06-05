using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PongPauseManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject pauseIcon;
    [SerializeField] private TMP_Text countdownText;

    [Header("Pause Panel")]
    [SerializeField] private GameObject pausePanel;

    [Header("Config")]
    [SerializeField] private int countdownSeconds = 3;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private bool paused;
    private Coroutine countdownRoutine;

    public bool IsPaused => paused;

    private void Start()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (pauseIcon != null)
            pauseIcon.SetActive(false);

        if (countdownText != null)
        {
            countdownText.text = "";
            countdownText.gameObject.SetActive(false);
        }
    }

    public void TogglePause()
    {
        if (paused) ResumeWithCountdown();
        else PauseGame();
    }

    public void PauseGame()
    {
        if (countdownRoutine != null)
        {
            StopCoroutine(countdownRoutine);
            countdownRoutine = null;
        }

        paused = true;
        Time.timeScale = 0f;

        if (pauseIcon != null) pauseIcon.SetActive(true);
        if (pausePanel != null) pausePanel.SetActive(true);

        if (countdownText != null)
        {
            countdownText.text = "";
            countdownText.gameObject.SetActive(false);
        }
    }

    public void ResumeWithCountdown()
    {
        if (countdownRoutine != null)
            StopCoroutine(countdownRoutine);

        countdownRoutine = StartCoroutine(ResumeRoutine());
    }

    private IEnumerator ResumeRoutine()
    {
        paused = true;
        Time.timeScale = 0f;

        if (pauseIcon != null) pauseIcon.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);

        if (countdownText != null)
            countdownText.gameObject.SetActive(true);

        int current = countdownSeconds;

        while (current > 0)
        {
            if (countdownText != null)
                countdownText.text = current.ToString();

            yield return new WaitForSecondsRealtime(1f);
            current--;
        }

        if (countdownText != null)
        {
            countdownText.text = "";
            countdownText.gameObject.SetActive(false);
        }

        paused = false;
        Time.timeScale = 1f;
        countdownRoutine = null;
    }

    public void OnClickContinue()
    {
        ResumeWithCountdown();
    }

    public void OnClickMainMenu()
    {
        Time.timeScale = 1f;
        paused = false;

        if (countdownRoutine != null)
        {
            StopCoroutine(countdownRoutine);
            countdownRoutine = null;
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }
}