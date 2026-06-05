using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TronBorderGlowUI : MonoBehaviour
{
    [Header("References (UI)")]
    [SerializeField] private RectTransform borderRect;
    [SerializeField] private RectTransform trailParent;

    [Header("Trail Prefab (UI Image)")]
    [SerializeField] private Image trailPrefab;

    [Header("Motion")]
    [SerializeField] private float lapSeconds = 1f;

    [Header("Trail")]
    [SerializeField] private float spawnEveryPixels = 0f;
    [SerializeField] private float trailLife = 0f;

    [Header("Color")]
    [SerializeField] private Color trailColor = Color.cyan;

    private Coroutine _run;
    private bool _visible = false;
    private bool _initialized = false;

    private void Awake()
    {
        _initialized = true;
        if (trailParent == null) trailParent = borderRect;
    }

    public void SetVisible(bool value)
    {
        _visible = value;

        if (_visible && _run == null)
        {
            _run = StartCoroutine(Run());
        }
    }

    public void SetColor(Color c)
    {
        trailColor = c;
    }

    private IEnumerator Run()
    {
        if (borderRect == null || trailPrefab == null)
            yield break;

        if (trailParent == null) trailParent = borderRect;

        while (true)
        {
            float w = borderRect.rect.width;
            float h = borderRect.rect.height;

            Vector2 topLeft = new Vector2(-w * 0.5f, h * 0.5f);
            Vector2 topRight = new Vector2(w * 0.5f, h * 0.5f);
            Vector2 bottomRight = new Vector2(w * 0.5f, -h * 0.5f);
            Vector2 bottomLeft = new Vector2(-w * 0.5f, -h * 0.5f);

            float perim = 2f * (w + h);
            float tHoriz = lapSeconds * (w / perim);
            float tVert = lapSeconds * (h / perim);

            yield return MoveSegment(topLeft, topRight, tHoriz);
            yield return MoveSegment(topRight, bottomRight, tVert);
            yield return MoveSegment(bottomRight, bottomLeft, tHoriz);
            yield return MoveSegment(bottomLeft, topLeft, tVert);
        }
    }

    private IEnumerator MoveSegment(Vector2 a, Vector2 b, float seconds)
    {
        seconds = Mathf.Max(0.0001f, seconds);

        float t = 0f;
        Vector2 lastSpawnPos = a;

        if (_visible) SpawnTrail(a);

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / seconds;
            Vector2 p = Vector2.Lerp(a, b, t);

            if (_visible)
            {
                float dist = Vector2.Distance(lastSpawnPos, p);
                if (dist >= Mathf.Max(0.01f, spawnEveryPixels))
                {
                    lastSpawnPos = p;
                    SpawnTrail(p);
                }
            }

            yield return null;
        }

        if (_visible) SpawnTrail(b);
    }

    private void SpawnTrail(Vector2 anchoredPos)
    {
        if (trailPrefab == null || trailParent == null) return;

        Image img = Instantiate(trailPrefab, trailParent);
        RectTransform rt = img.rectTransform;

        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;

        img.color = trailColor;

        if (trailLife <= 0.0001f)
        {
            Destroy(img.gameObject, 0.01f);
        }
        else
        {
            StartCoroutine(FadeAndKill(img));
        }
    }

    private IEnumerator FadeAndKill(Image img)
    {
        float t = 0f;
        Color c = img.color;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / trailLife;
            c.a = Mathf.Lerp(1f, 0f, t);
            img.color = c;
            yield return null;
        }

        Destroy(img.gameObject);
    }
}