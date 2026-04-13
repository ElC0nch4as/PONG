using System.Collections;
using TMPro;
using UnityEngine;

public class PongPauseManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject pauseIcon;
    [SerializeField] private TMP_Text countdownText;

    [Header("Config")]
    [SerializeField] private int countdownSeconds = 3;

    private bool paused;
    private Coroutine countdownRoutine;

    public bool IsPaused
    {
        get { return paused; }
    }

    public void TogglePause()
    {
        if (paused)
        {
            ResumeWithCountdown();
        }
        else
        {
            PauseGame();
        }
    }

    private void PauseGame()
    {
        if (countdownRoutine != null)
        {
            StopCoroutine(countdownRoutine);
            countdownRoutine = null;
        }

        paused = true;
        Time.timeScale = 0f;

        if (pauseIcon != null)
        {
            pauseIcon.SetActive(true);
        }

        if (countdownText != null)
        {
            countdownText.text = "";
            countdownText.gameObject.SetActive(false);
        }
    }

    private void ResumeWithCountdown()
    {
        if (countdownRoutine != null)
        {
            StopCoroutine(countdownRoutine);
        }

        countdownRoutine = StartCoroutine(ResumeRoutine());
    }

    private IEnumerator ResumeRoutine()
    {
        paused = true;
        Time.timeScale = 0f;

        if (pauseIcon != null)
        {
            pauseIcon.SetActive(false);
        }

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
        }

        int current = countdownSeconds;

        while (current > 0)
        {
            if (countdownText != null)
            {
                countdownText.text = current.ToString();
            }

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
}