using System.Collections;
using TMPro;
using UnityEngine;

public class PongGameManagerCore : MonoBehaviour, IPongRestartHandler
{
    public static PongGameManagerCore Instance { get; private set; }

    public enum GameMode { Tron, Classic, TwoVsTwo }

    [Header("Mode")]
    [SerializeField] private GameMode mode = GameMode.Tron;

    [Header("Win Rules")]
    [SerializeField] private int classicPointsToWin = 11;
    [SerializeField] private int tronPointsToWin = 5;
    [SerializeField] private int twoVsTwoPointsToWin = 10;

    [Header("Tron Phase2 Trigger")]
    [SerializeField] private int phase2Score = 3;

    [Header("Phase 2 Rules (Tron)")]
    [SerializeField] private int pointsToWinPhase2 = 8;

    [Header("UI")]
    [SerializeField] private TMP_Text p1ScoreText;
    [SerializeField] private TMP_Text p2ScoreText;
    [SerializeField] private TMP_Text winnerText;
    [SerializeField] private GameObject linePhase1;
    [SerializeField] private GameObject linePhase2;

    [Header("References")]
    [SerializeField] private Ball ball;
    [SerializeField] private CameraExpand cameraExpand;
    [SerializeField] private PongMusicManager musicManager;

    [Header("Paddles")]
    [SerializeField] private Paddle leftPaddle;
    [SerializeField] private Paddle rightPaddle;
    [SerializeField] private Paddle topPaddle;
    [SerializeField] private Paddle bottomPaddle;

    [Header("Phase2 Positions")]
    [SerializeField] private float topY = 6.5f;
    [SerializeField] private float bottomY = -6.5f;
    [SerializeField] private float sidePadding = 0.6f;

    [Header("Phase 1 Speeds")]
    [SerializeField] private float leftPaddleSpeedPhase1 = 10f;
    [SerializeField] private float rightPaddleSpeedPhase1 = 10f;

    [Header("Phase 2 Speeds")]
    [SerializeField] private float leftPaddleSpeedPhase2 = 15f;
    [SerializeField] private float rightPaddleSpeedPhase2 = 15f;
    [SerializeField] private float topPaddleSpeedPhase2 = 20f;
    [SerializeField] private float bottomPaddleSpeedPhase2 = 20f;

    private int player1Score;
    private int player2Score;

    private bool phase2Activated;
    private bool gameEnded;

    private int pointsToWin;
    private float queuedServeX;

    private Vector3 leftStartPos;
    private Vector3 rightStartPos;
    private Vector3 topStartPos;
    private Vector3 bottomStartPos;

    public bool IsGameEnded => gameEnded;
    public bool IsPhase2Transitioning { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (cameraExpand != null) cameraExpand.ResetToNormal(true);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        SaveStartPositions();
        ApplyModeSettings();
        SetupInitialScene();

        if (mode == GameMode.TwoVsTwo) ForceStartAsPhase2();
        else ApplyPhase1Speeds();

        UpdateScoreUI();
        SetWinnerText("");

        if (ball != null)
        {
            ball.CancelPendingLaunch();
            ball.ResetBallToCenter();
            if (Camera.main != null) ball.ApplyBoundsFromCamera(Camera.main, 0.3f);
            ball.BeginLaunch();
        }

        if (musicManager != null)
        {
            if (mode == GameMode.Tron) musicManager.PlayTron();
            else if (mode == GameMode.TwoVsTwo) musicManager.PlayTwoVsTwo();
        }
    }

    private void ApplyModeSettings()
    {
        bool classic = (mode == GameMode.Classic);
        bool twoVsTwo = (mode == GameMode.TwoVsTwo);

        if (classic) pointsToWin = classicPointsToWin;
        else if (twoVsTwo) pointsToWin = twoVsTwoPointsToWin;
        else pointsToWin = tronPointsToWin;

        if (classic)
        {
            phase2Activated = false;
            IsPhase2Transitioning = false;
        }

        if (topPaddle != null) topPaddle.gameObject.SetActive(false);
        if (bottomPaddle != null) bottomPaddle.gameObject.SetActive(false);

        if (ball != null)
        {
            if (classic) ball.DisableFourPaddlesMode();
            else if (twoVsTwo) ball.EnableFourPaddlesMode();
            else ball.DisableFourPaddlesMode();

            ball.SetSpeedIncreasePerHit(classic ? 1f : 1.05f);

            if (Camera.main != null) ball.ApplyBoundsFromCamera(Camera.main, 0.3f);
        }

        if (classic && cameraExpand != null)
            cameraExpand.ResetToNormal(true);
    }

    private void SetupInitialScene()
    {
        if (linePhase1 != null) linePhase1.SetActive(true);
        if (linePhase2 != null) linePhase2.SetActive(false);
    }

    private void ForceStartAsPhase2()
    {
        phase2Activated = true;
        IsPhase2Transitioning = false;

        if (linePhase1 != null) linePhase1.SetActive(false);
        if (linePhase2 != null) linePhase2.SetActive(true);

        if (topPaddle != null)
        {
            topPaddle.gameObject.SetActive(true);
            var p = topPaddle.transform.position; p.y = topY; topPaddle.transform.position = p;
        }

        if (bottomPaddle != null)
        {
            bottomPaddle.gameObject.SetActive(true);
            var p = bottomPaddle.transform.position; p.y = bottomY; bottomPaddle.transform.position = p;
        }

        MoveSidePaddlesToEdges();
        ApplyClampsToFitCamera(0f);
        ApplyNoOverlapClampsPhase2();
        ApplyPhase2Speeds();

        if (ball != null)
        {
            ball.EnableFourPaddlesMode();
            if (Camera.main != null) ball.ApplyBoundsFromCamera(Camera.main, 0.3f);
        }
    }

    public void AddPointToPlayer1()
    {
        if (gameEnded) return;

        player1Score++;
        UpdateScoreUI();

        if (WillTriggerPhase2Now())
        {
            BeginPhase2Transition(-1f);
            return;
        }

        if (musicManager != null) musicManager.PlayPointSfx();

        CheckWinner();
    }

    public void AddPointToPlayer2()
    {
        if (gameEnded) return;

        player2Score++;
        UpdateScoreUI();

        if (WillTriggerPhase2Now())
        {
            BeginPhase2Transition(1f);
            return;
        }

        if (musicManager != null) musicManager.PlayPointSfx();

        CheckWinner();
    }

    private bool WillTriggerPhase2Now()
    {
        if (mode != GameMode.Tron) return false;
        if (phase2Activated) return false;

        return player1Score == phase2Score && player2Score == phase2Score;
    }

    private void BeginPhase2Transition(float serveX)
    {
        if (IsPhase2Transitioning) return;

        phase2Activated = true;
        IsPhase2Transitioning = true;
        queuedServeX = serveX;

        if (ball != null)
        {
            ball.StopBall();
            ball.CancelPendingLaunch();
            ball.ResetBallToCenter();
        }

        StartCoroutine(Phase2Routine());
    }

    private IEnumerator Phase2Routine()
    {
        if (linePhase1 != null) linePhase1.SetActive(false);
        if (linePhase2 != null) linePhase2.SetActive(true);

        if (musicManager != null) musicManager.StopMusicAndAmbience();

        if (topPaddle != null)
        {
            topPaddle.gameObject.SetActive(true);
            var p = topPaddle.transform.position; p.y = topY; topPaddle.transform.position = p;
            topPaddle.enabled = false;
        }

        if (bottomPaddle != null)
        {
            bottomPaddle.gameObject.SetActive(true);
            var p = bottomPaddle.transform.position; p.y = bottomY; bottomPaddle.transform.position = p;
            bottomPaddle.enabled = false;
        }

        if (cameraExpand != null) cameraExpand.Expand();

        float wait = (cameraExpand != null) ? cameraExpand.TotalTransitionTime : 0f;
        yield return new WaitForSecondsRealtime(wait);

        if (musicManager != null)
        {
            musicManager.PlayPhase2();
            musicManager.ResumeAmbience();
        }

        if (ball != null)
        {
            ball.EnableFourPaddlesMode();
            if (Camera.main != null) ball.ApplyBoundsFromCamera(Camera.main, 0.3f);
        }

        MoveSidePaddlesToEdges();
        ApplyClampsToFitCamera(0f);
        ApplyNoOverlapClampsPhase2();
        ApplyPhase2Speeds();

        if (topPaddle != null) topPaddle.enabled = true;
        if (bottomPaddle != null) bottomPaddle.enabled = true;

        pointsToWin = pointsToWinPhase2;

        if (ball != null && !gameEnded)
            ball.BeginLaunchInstant(queuedServeX);

        queuedServeX = 0f;
        IsPhase2Transitioning = false;
    }

    public void TryRestartGame()
    {
        if (!gameEnded) return;

        player1Score = 0;
        player2Score = 0;
        gameEnded = false;

        phase2Activated = false;
        IsPhase2Transitioning = false;
        queuedServeX = 0f;

        ApplyModeSettings();
        SetupInitialScene();
        RestorePaddlePositions();

        if (mode == GameMode.TwoVsTwo) ForceStartAsPhase2();
        else ApplyPhase1Speeds();

        UpdateScoreUI();
        SetWinnerText("");

        if (cameraExpand != null) cameraExpand.ResetToNormal(true);

        ApplyClampsToFitCamera(0f);

        if (ball != null)
        {
            ball.CancelPendingLaunch();
            ball.ResetBallToCenter();
            if (Camera.main != null) ball.ApplyBoundsFromCamera(Camera.main, 0.3f);
            ball.BeginLaunch();
        }

        if (musicManager != null)
        {
            if (mode == GameMode.Tron) musicManager.PlayTron();
            else if (mode == GameMode.TwoVsTwo) musicManager.PlayTwoVsTwo();
        }
    }

    public void QueueServe(float x) => queuedServeX = Mathf.Sign(x);

    private void SaveStartPositions()
    {
        if (leftPaddle != null) leftStartPos = leftPaddle.transform.position;
        if (rightPaddle != null) rightStartPos = rightPaddle.transform.position;
        if (topPaddle != null) topStartPos = topPaddle.transform.position;
        if (bottomPaddle != null) bottomStartPos = bottomPaddle.transform.position;
    }

    private void RestorePaddlePositions()
    {
        if (leftPaddle != null) leftPaddle.transform.position = leftStartPos;
        if (rightPaddle != null) rightPaddle.transform.position = rightStartPos;
        if (topPaddle != null) topPaddle.transform.position = topStartPos;
        if (bottomPaddle != null) bottomPaddle.transform.position = bottomStartPos;
    }

    private void ApplyPhase1Speeds()
    {
        if (leftPaddle != null) leftPaddle.SetSpeed(leftPaddleSpeedPhase1);
        if (rightPaddle != null) rightPaddle.SetSpeed(rightPaddleSpeedPhase1);
    }

    private void ApplyPhase2Speeds()
    {
        if (leftPaddle != null) leftPaddle.SetSpeed(leftPaddleSpeedPhase2);
        if (rightPaddle != null) rightPaddle.SetSpeed(rightPaddleSpeedPhase2);
        if (topPaddle != null) topPaddle.SetSpeed(topPaddleSpeedPhase2);
        if (bottomPaddle != null) bottomPaddle.SetSpeed(bottomPaddleSpeedPhase2);
    }

    private void MoveSidePaddlesToEdges()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        if (leftPaddle != null)
        {
            float halfP = GetHalfWidth(leftPaddle.transform);
            var p = leftPaddle.transform.position;
            p.x = -halfW + sidePadding + halfP;
            leftPaddle.transform.position = p;
        }

        if (rightPaddle != null)
        {
            float halfP = GetHalfWidth(rightPaddle.transform);
            var p = rightPaddle.transform.position;
            p.x = halfW - sidePadding - halfP;
            rightPaddle.transform.position = p;
        }
    }

    private void ApplyClampsToFitCamera(float padding)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        if (leftPaddle != null && rightPaddle != null)
        {
            float halfPH = GetHalfHeight(leftPaddle.transform);
            float minY = -halfH + padding + halfPH;
            float maxY = halfH - padding - halfPH;
            leftPaddle.SetClamp(minY, maxY);
            rightPaddle.SetClamp(minY, maxY);
        }

        if (topPaddle != null && bottomPaddle != null)
        {
            float halfPW = GetHalfWidth(topPaddle.transform);
            float minX = -halfW + padding + halfPW;
            float maxX = halfW - padding - halfPW;
            topPaddle.SetClamp(minX, maxX);
            bottomPaddle.SetClamp(minX, maxX);
        }
    }

    private void ApplyNoOverlapClampsPhase2()
    {
        if (leftPaddle == null || rightPaddle == null || topPaddle == null || bottomPaddle == null) return;

        float halfLeftW = GetHalfWidth(leftPaddle.transform);
        float halfRightW = GetHalfWidth(rightPaddle.transform);
        float halfTopW = GetHalfWidth(topPaddle.transform);

        float halfLeftH = GetHalfHeight(leftPaddle.transform);
        float halfTopH = GetHalfHeight(topPaddle.transform);
        float halfBottomH = GetHalfHeight(bottomPaddle.transform);

        float minX = leftPaddle.transform.position.x + halfLeftW + halfTopW;
        float maxX = rightPaddle.transform.position.x - halfRightW - halfTopW;
        topPaddle.SetClamp(minX, maxX);
        bottomPaddle.SetClamp(minX, maxX);

        float minY = bottomPaddle.transform.position.y + halfBottomH + halfLeftH;
        float maxY = topPaddle.transform.position.y - halfTopH - halfLeftH;
        leftPaddle.SetClamp(minY, maxY);
        rightPaddle.SetClamp(minY, maxY);
    }

    private float GetHalfWidth(Transform t)
    {
        var sr = t.GetComponent<SpriteRenderer>();
        return sr != null ? sr.bounds.extents.x : 0.2f;
    }

    private float GetHalfHeight(Transform t)
    {
        var sr = t.GetComponentInChildren<SpriteRenderer>();
        return sr != null ? sr.bounds.extents.y : 0.2f;
    }

    private void CheckWinner()
    {
        if (player1Score >= pointsToWin) { EndGame("Player 1 Wins! Press Space to Restart"); return; }
        if (player2Score >= pointsToWin) { EndGame("Player 2 Wins! Press Space to Restart"); }
    }

    private void EndGame(string msg)
    {
        gameEnded = true;
        SetWinnerText(msg);
        if (ball != null) ball.StopBall();
    }

    private void UpdateScoreUI()
    {
        if (p1ScoreText != null) p1ScoreText.text = player1Score.ToString();
        if (p2ScoreText != null) p2ScoreText.text = player2Score.ToString();
    }

    private void SetWinnerText(string msg)
    {
        if (winnerText != null) winnerText.text = msg;
    }
}