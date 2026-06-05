using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraExpand : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float normalSize = 5.05f;
    [SerializeField] private float expandedSize = 7.5f;
    [SerializeField] private float duration = 5f;

    [Header("SFX")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip expandSfx;

    [SerializeField] private float extraHoldAfterExpand = 2f;

    public float TotalTransitionTime
    {
        get
        {
            float sfx = (expandSfx != null) ? expandSfx.length : 0f;
            float baseTime = Mathf.Max(duration, sfx);
            return baseTime + extraHoldAfterExpand;
        }
    }

    private Coroutine sizeRoutine;
    private bool playedExpandSfx;

    public float Duration => duration;
    public float ExpandedSize => expandedSize;

    public float ExpandSfxLength => (expandSfx != null) ? expandSfx.length : 0f;

    private void Awake()
    {
        if (targetCamera == null) targetCamera = GetComponent<Camera>();
        if (targetCamera == null) targetCamera = Camera.main;

        ForceNormalNow();
    }

    private void OnEnable()
    {
        ForceNormalNow();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ForceNormalNow();
        playedExpandSfx = false;
    }

    private void ForceNormalNow()
    {
        if (targetCamera == null) return;

        if (sizeRoutine != null)
        {
            StopCoroutine(sizeRoutine);
            sizeRoutine = null;
        }

        targetCamera.orthographicSize = normalSize;
    }

    public void Expand()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null) return;

        if (!playedExpandSfx && sfxSource != null && expandSfx != null)
        {
            playedExpandSfx = true;
            sfxSource.PlayOneShot(expandSfx);
        }

        StartSizeRoutine(expandedSize);
    }

    public void ResetToNormal(bool instant = true)
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null) return;

        playedExpandSfx = false;

        if (sizeRoutine != null)
        {
            StopCoroutine(sizeRoutine);
            sizeRoutine = null;
        }

        if (instant)
        {
            targetCamera.orthographicSize = normalSize;
        }
        else
        {
            StartSizeRoutine(normalSize);
        }
    }

    private void StartSizeRoutine(float targetSize)
    {
        if (sizeRoutine != null) StopCoroutine(sizeRoutine);
        sizeRoutine = StartCoroutine(LerpSizeRoutine(targetSize));
    }

    private IEnumerator LerpSizeRoutine(float targetSize)
    {
        float startSize = targetCamera.orthographicSize;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            targetCamera.orthographicSize = Mathf.Lerp(startSize, targetSize, t);
            yield return null;
        }

        targetCamera.orthographicSize = targetSize;
        sizeRoutine = null;
    }
}