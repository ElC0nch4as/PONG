using System.Collections;
using UnityEngine;

public class CameraExpand : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float normalSize = 5f;
    [SerializeField] private float expandedSize = 7.5f;
    [SerializeField] private float duration = 5f;

    [Header("SFX")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip expandSfx;

    private Coroutine sizeRoutine;
    private bool playedExpandSfx;

    public float Duration
    {
        get { return duration; }
    }

    public float ExpandedSize
    {
        get { return expandedSize; }
    }

    public float ExpandSfxLength
    {
        get
        {
            if (expandSfx == null)
            {
                return 0f;
            }

            return expandSfx.length;
        }
    }

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera != null)
        {
            targetCamera.orthographicSize = normalSize;
        }
    }

    public void Expand()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            return;
        }

        if (!playedExpandSfx && sfxSource != null && expandSfx != null)
        {
            playedExpandSfx = true;
            sfxSource.PlayOneShot(expandSfx);
        }

        StartSizeRoutine(expandedSize);
    }

    public void ResetToNormal()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            return;
        }

        if (sizeRoutine != null)
        {
            StopCoroutine(sizeRoutine);
            sizeRoutine = null;
        }

        targetCamera.orthographicSize = normalSize;
        playedExpandSfx = false;
    }

    private void StartSizeRoutine(float targetSize)
    {
        if (sizeRoutine != null)
        {
            StopCoroutine(sizeRoutine);
        }

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