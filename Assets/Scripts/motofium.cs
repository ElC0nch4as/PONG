using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class motofium : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private RectTransform trailParent;
    [SerializeField] private Image trailPrefab;

    [Header("Moto (solo posición)")]
    [SerializeField] private RectTransform moto;

    [Header("Path")]
    [SerializeField] private int minTurns = 1;
    [SerializeField] private int maxTurns = 3;
    [SerializeField] private float margin = 40f;

    [Header("Tiempo por recorrido (cruzar pantalla)")]
    [SerializeField] private float minTravelSeconds = 6f;
    [SerializeField] private float maxTravelSeconds = 10f;

    [Header("Colores posibles")]
    [SerializeField] private Color[] possibleColors;

    [Header("Trail")]
    [Tooltip("Distancia en pixeles entre puntos")]
    [SerializeField] private float spawnEveryPixels = 10f;

    [Tooltip("Cuánto vive cada punto")]
    [SerializeField] private float trailLife = 0.8f;

    [Tooltip("Límite duro: cuántos puntos máx se crean por segundo")]
    [SerializeField] private float maxTrailsPerSec = 120f;

    [Header("Pool (anti-crash)")]
    [SerializeField] private int poolSize = 400;

    private Coroutine _runner;
    private float _spawnAccumDist;
    private float _spawnCooldown; 
    private Vector2 _lastSpawnPos;

    private readonly Queue<Image> _pool = new Queue<Image>();
    private readonly List<ActiveTrail> _active = new List<ActiveTrail>(1024);

    private struct ActiveTrail
    {
        public Image img;
        public float born;
        public float life;
        public Color baseColor;
    }

    private void Awake()
    {
        if (trailParent == null) trailParent = canvasRect;
        if (moto == null) moto = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        BuildPoolIfNeeded();

        if (_runner != null) StopCoroutine(_runner);
        _runner = StartCoroutine(RunForever());
    }

    private void OnDisable()
    {
        if (_runner != null)
        {
            StopCoroutine(_runner);
            _runner = null;
        }

        ClearAllTrails();
    }

    private void Update()
    {
        float now = Time.unscaledTime;
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            var t = _active[i];
            float u = (now - t.born) / Mathf.Max(0.0001f, t.life);
            if (u >= 1f)
            {
                Recycle(t.img);
                _active.RemoveAt(i);
                continue;
            }

            Color c = t.baseColor;
            c.a = Mathf.Lerp(1f, 0f, u);
            t.img.color = c;
            _active[i] = t;
        }
    }

    private IEnumerator RunForever()
    {
        if (canvasRect == null || trailPrefab == null)
            yield break;

        if (trailParent == null) trailParent = canvasRect;

        while (true)
        {
            Color c = PickColor();

            yield return RunOnePath(c);

            yield return new WaitForSecondsRealtime(Random.Range(0.2f, 0.6f));
        }
    }

    private IEnumerator RunOnePath(Color c)
    {
        float w = canvasRect.rect.width;
        float h = canvasRect.rect.height;

        int side = Random.Range(0, 4);
        Vector2 start = GetPointOnSide(side, w, h, margin);
        Vector2 end = GetPointOnSide(Opposite(side), w, h, margin);

        int turns = Random.Range(minTurns, maxTurns + 1);
        Vector2[] pts = BuildOrthogonalPath(start, end, turns, w, h, margin);

        float travelSeconds = Random.Range(minTravelSeconds, maxTravelSeconds);

        float totalLen = 0f;
        for (int i = 0; i < pts.Length - 1; i++)
            totalLen += Vector2.Distance(pts[i], pts[i + 1]);

        float pxPerSec = totalLen / Mathf.Max(0.1f, travelSeconds);

        moto.anchoredPosition = start;
        _lastSpawnPos = start;
        _spawnAccumDist = 0f;
        _spawnCooldown = 0f;

        SpawnTrail(start, c);

        for (int i = 0; i < pts.Length - 1; i++)
            yield return MoveSegment(pts[i], pts[i + 1], pxPerSec, c);

        SpawnTrail(end, c);
    }

    private IEnumerator MoveSegment(Vector2 a, Vector2 b, float pxPerSec, Color c)
    {
        float dist = Vector2.Distance(a, b);
        if (dist <= 0.001f) yield break;

        float t = 0f;

        while (t < 1f)
        {
            float dt = Time.unscaledDeltaTime;
            float step = (pxPerSec * dt) / dist;
            t = Mathf.Clamp01(t + step);

            Vector2 p = Vector2.Lerp(a, b, t);
            moto.anchoredPosition = p;

            SpawnByDistanceAndRate(p, c, dt);

            yield return null;
        }
    }

    private void SpawnByDistanceAndRate(Vector2 p, Color c, float dt)
    {
        float d = Vector2.Distance(_lastSpawnPos, p);
        _spawnAccumDist += d;

        if (maxTrailsPerSec <= 0f) maxTrailsPerSec = 60f;
        _spawnCooldown -= dt;

        if (_spawnAccumDist >= spawnEveryPixels && _spawnCooldown <= 0f)
        {
            _spawnAccumDist = 0f;
            _lastSpawnPos = p;

            _spawnCooldown = 1f / maxTrailsPerSec;

            SpawnTrail(p, c);
        }
    }

    private void SpawnTrail(Vector2 anchoredPos, Color c)
    {
        if (trailLife <= 0f) return;

        Image img = GetFromPool();
        RectTransform rt = img.rectTransform;

        rt.SetParent(trailParent, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;

        img.color = c;

        _active.Add(new ActiveTrail
        {
            img = img,
            born = Time.unscaledTime,
            life = trailLife,
            baseColor = c
        });
    }

    private void BuildPoolIfNeeded()
    {
        if (_pool.Count > 0 || trailPrefab == null) return;

        for (int i = 0; i < poolSize; i++)
        {
            Image img = Instantiate(trailPrefab, trailParent);
            img.gameObject.SetActive(false);
            _pool.Enqueue(img);
        }
    }

    private Image GetFromPool()
    {
        if (_pool.Count == 0)
        {
            Image extra = Instantiate(trailPrefab, trailParent);
            return extra;
        }

        Image img = _pool.Dequeue();
        img.gameObject.SetActive(true);
        return img;
    }

    private void Recycle(Image img)
    {
        if (img == null) return;
        img.gameObject.SetActive(false);
        _pool.Enqueue(img);
    }

    private void ClearAllTrails()
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            Recycle(_active[i].img);
        }
        _active.Clear();
    }

    private Color PickColor()
    {
        if (possibleColors != null && possibleColors.Length > 0)
            return possibleColors[Random.Range(0, possibleColors.Length)];

        return Color.cyan;
    }
    private static int Opposite(int side) => side switch
    {
        0 => 1,
        1 => 0,
        2 => 3,
        _ => 2
    };

    private static Vector2 GetPointOnSide(int side, float w, float h, float m)
    {
        float xMin = -w * 0.5f + m;
        float xMax = w * 0.5f - m;
        float yMin = -h * 0.5f + m;
        float yMax = h * 0.5f - m;

        return side switch
        {
            0 => new Vector2(xMin, Random.Range(yMin, yMax)),
            1 => new Vector2(xMax, Random.Range(yMin, yMax)),
            2 => new Vector2(Random.Range(xMin, xMax), yMax),
            _ => new Vector2(Random.Range(xMin, xMax), yMin),
        };
    }

    private static Vector2[] BuildOrthogonalPath(Vector2 start, Vector2 end, int turns, float w, float h, float m)
    {
        float xMin = -w * 0.5f + m;
        float xMax = w * 0.5f - m;
        float yMin = -h * 0.5f + m;
        float yMax = h * 0.5f - m;

        Vector2[] pts = new Vector2[turns + 2];
        pts[0] = start;
        pts[pts.Length - 1] = end;

        bool moveX = Random.value < 0.5f;
        Vector2 cur = start;

        for (int i = 1; i <= turns; i++)
        {
            Vector2 next;
            if (moveX)
                next = new Vector2(Random.Range(xMin, xMax), cur.y);
            else
                next = new Vector2(cur.x, Random.Range(yMin, yMax));

            pts[i] = next;
            cur = next;
            moveX = !moveX;
        }

        Vector2 preEnd = pts[pts.Length - 2];
        if (Mathf.Abs(preEnd.x - end.x) > 0.1f && Mathf.Abs(preEnd.y - end.y) > 0.1f)
        {
            if (Random.value < 0.5f)
                pts[pts.Length - 2] = new Vector2(end.x, preEnd.y);
            else
                pts[pts.Length - 2] = new Vector2(preEnd.x, end.y);
        }

        return pts;
    }
}