using System.Collections;
using TMPro;
using UnityEngine;

public class PongGameManagerCore : MonoBehaviour, IPongRestartHandler
{
    public static PongGameManagerCore Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TMP_Text p1ScoreText;
    [SerializeField] private TMP_Text p2ScoreText;
    [SerializeField] private TMP_Text winnerText;
    [SerializeField] private GameObject linePhase1;
    [SerializeField] private GameObject linePhase2;

    [Header("Main References")]
    [SerializeField] private Ball ball;
    [SerializeField] private CameraExpand cameraExpand;
    [SerializeField] private MusicPhase2 musicManager;

    [Header("Paddles")]
    [SerializeField] private Paddle leftPaddle;
    [SerializeField] private Paddle rightPaddle;
    [SerializeField] private Paddle topPaddle;
    [SerializeField] private Paddle bottomPaddle;

    [Header("Rules")]
    [SerializeField] private int pointsToWin = 5;
    [SerializeField] private int phase2Score = 3;

    [Header("Phase 2 Positions")]
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

    [Header("Phase 2 Rules")]
    [SerializeField] private int pointsToWinPhase2 = 8;
    [SerializeField] private float extraPauseAfterExpand = 2f;

    private int player1Score;
    private int player2Score;
    private int basePointsToWin;

    private bool phase2Activated;
    private bool gameEnded;

    private float queuedServeX;

    private Vector3 leftStartPos;
    private Vector3 rightStartPos;
    private Vector3 topStartPos;
    private Vector3 bottomStartPos;

    public bool IsGameEnded
    {
        get { return gameEnded; }
    }

    public bool IsPhase2Transitioning { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        basePointsToWin = pointsToWin;

        SaveStartPositions();
        SetupInitialScene();
        ApplyPhase1Speeds();
        UpdateScoreUI();
        SetWinnerText("");
    }

    public void AddPointToPlayer1()
    {
        if (gameEnded)
        {
            return;
        }

        player1Score++;
        UpdateScoreUI();
        TryActivatePhase2();
        CheckWinner();
    }

    public void AddPointToPlayer2()
    {
        if (gameEnded)
        {
            return;
        }

        player2Score++;
        UpdateScoreUI();
        TryActivatePhase2();
        CheckWinner();
    }

    public void QueueServe(float x)
    {
        queuedServeX = Mathf.Sign(x);
    }

    public void TryRestartGame()
    {
        if (!gameEnded)
        {
            return;
        }

        if (topPaddle != null)
        {
            topPaddle.enabled = true;
        }

        if (bottomPaddle != null)
        {
            bottomPaddle.enabled = true;
        }

        player1Score = 0;
        player2Score = 0;
        gameEnded = false;
        phase2Activated = false;
        queuedServeX = 0f;
        IsPhase2Transitioning = false;
        pointsToWin = basePointsToWin;

        UpdateScoreUI();
        SetWinnerText("");

        SetupInitialScene();
        RestorePaddlePositions();
        ApplyPhase1Speeds();

        if (cameraExpand != null)
        {
            cameraExpand.ResetToNormal();
        }

        ApplyClampsToFitCamera(0f);

        if (ball != null)
        {
            ball.DisableFourPaddlesMode();
            ball.CancelPendingLaunch();
            ball.ResetBallToCenter();
            ball.ApplyBoundsFromCamera(Camera.main, 0.3f);
            ball.BeginLaunch();
        }

        if (musicManager != null)
        {
            musicManager.PlayNormal();
        }
    }

    private void SaveStartPositions()
    {
        if (leftPaddle != null)
        {
            leftStartPos = leftPaddle.transform.position;
        }

        if (rightPaddle != null)
        {
            rightStartPos = rightPaddle.transform.position;
        }

        if (topPaddle != null)
        {
            topStartPos = topPaddle.transform.position;
        }

        if (bottomPaddle != null)
        {
            bottomStartPos = bottomPaddle.transform.position;
        }
    }

    private void SetupInitialScene()
    {
        if (linePhase1 != null)
        {
            linePhase1.SetActive(true);
        }

        if (linePhase2 != null)
        {
            linePhase2.SetActive(false);
        }

        if (topPaddle != null)
        {
            topPaddle.gameObject.SetActive(false);
        }

        if (bottomPaddle != null)
        {
            bottomPaddle.gameObject.SetActive(false);
        }
    }

    private void RestorePaddlePositions()
    {
        if (leftPaddle != null)
        {
            leftPaddle.transform.position = leftStartPos;
        }

        if (rightPaddle != null)
        {
            rightPaddle.transform.position = rightStartPos;
        }

        if (topPaddle != null)
        {
            topPaddle.transform.position = topStartPos;
        }

        if (bottomPaddle != null)
        {
            bottomPaddle.transform.position = bottomStartPos;
        }
    }

    private void ApplyPhase1Speeds()
    {
        if (leftPaddle != null)
        {
            leftPaddle.SetSpeed(leftPaddleSpeedPhase1);
        }

        if (rightPaddle != null)
        {
            rightPaddle.SetSpeed(rightPaddleSpeedPhase1);
        }
    }

    private void ApplyPhase2Speeds()
    {
        if (leftPaddle != null)
        {
            leftPaddle.SetSpeed(leftPaddleSpeedPhase2);
        }

        if (rightPaddle != null)
        {
            rightPaddle.SetSpeed(rightPaddleSpeedPhase2);
        }

        if (topPaddle != null)
        {
            topPaddle.SetSpeed(topPaddleSpeedPhase2);
        }

        if (bottomPaddle != null)
        {
            bottomPaddle.SetSpeed(bottomPaddleSpeedPhase2);
        }
    }

    private void TryActivatePhase2()
    {
        if (phase2Activated)
        {
            return;
        }

        if (player1Score == phase2Score && player2Score == phase2Score)
        {
            StartCoroutine(Phase2Routine());
        }
    }

    private IEnumerator Phase2Routine()
    {
        phase2Activated = true;
        IsPhase2Transitioning = true;

        if (linePhase1 != null)
        {
            linePhase1.SetActive(false);
        }

        if (linePhase2 != null)
        {
            linePhase2.SetActive(true);
        }

        if (musicManager != null)
        {
            musicManager.StopNormalOnly();
        }

        if (ball != null)
        {
            ball.StopBall();
            ball.CancelPendingLaunch();
        }

        if (topPaddle != null)
        {
            topPaddle.gameObject.SetActive(true);

            Vector3 topPosition = topPaddle.transform.position;
            topPosition.y = topY;
            topPaddle.transform.position = topPosition;

            topPaddle.enabled = false;
        }

        if (bottomPaddle != null)
        {
            bottomPaddle.gameObject.SetActive(true);

            Vector3 bottomPosition = bottomPaddle.transform.position;
            bottomPosition.y = bottomY;
            bottomPaddle.transform.position = bottomPosition;

            bottomPaddle.enabled = false;
        }

        if (cameraExpand != null)
        {
            cameraExpand.Expand();
        }

        float expandTime = 0.5f;
        float sfxTime = 0f;
        float totalWait;

        if (cameraExpand != null)
        {
            expandTime = cameraExpand.Duration;
            sfxTime = cameraExpand.ExpandSfxLength;
        }

        totalWait = Mathf.Max(expandTime, sfxTime) + extraPauseAfterExpand;

        yield return new WaitForSecondsRealtime(totalWait);

        if (musicManager != null)
        {
            musicManager.PlayPhase2();
        }

        if (ball != null)
        {
            ball.EnableFourPaddlesMode();
            ball.ApplyBoundsFromCamera(Camera.main, 0.3f);
        }

        MoveSidePaddlesToEdges();
        ApplyClampsToFitCamera(0f);
        ApplyNoOverlapClampsPhase2();
        ApplyPhase2Speeds();

        if (topPaddle != null)
        {
            topPaddle.enabled = true;
        }

        if (bottomPaddle != null)
        {
            bottomPaddle.enabled = true;
        }

        pointsToWin = pointsToWinPhase2;

        if (ball != null && !gameEnded)
        {
            if (queuedServeX == 0f)
            {
                ball.BeginLaunchInstant(0f);
            }
            else
            {
                ball.BeginLaunchInstant(queuedServeX);
            }
        }

        queuedServeX = 0f;
        IsPhase2Transitioning = false;
    }

    private void ActivateTopAndBottomPaddles()
    {
        if (topPaddle != null)
        {
            topPaddle.gameObject.SetActive(true);

            Vector3 topPosition = topPaddle.transform.position;
            topPosition.y = topY;
            topPaddle.transform.position = topPosition;
        }

        if (bottomPaddle != null)
        {
            bottomPaddle.gameObject.SetActive(true);

            Vector3 bottomPosition = bottomPaddle.transform.position;
            bottomPosition.y = bottomY;
            bottomPaddle.transform.position = bottomPosition;
        }
    }

    private void MoveSidePaddlesToEdges()
    {
        Camera cam = Camera.main;

        if (cam == null)
        {
            return;
        }

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        if (leftPaddle != null)
        {
            float halfPaddleWidth = GetHalfWidth(leftPaddle.transform);
            Vector3 leftPosition = leftPaddle.transform.position;
            leftPosition.x = -halfWidth + sidePadding + halfPaddleWidth;
            leftPaddle.transform.position = leftPosition;
        }

        if (rightPaddle != null)
        {
            float halfPaddleWidth = GetHalfWidth(rightPaddle.transform);
            Vector3 rightPosition = rightPaddle.transform.position;
            rightPosition.x = halfWidth - sidePadding - halfPaddleWidth;
            rightPaddle.transform.position = rightPosition;
        }
    }

    private void ApplyClampsToFitCamera(float padding)
    {
        Camera cam = Camera.main;

        if (cam == null)
        {
            return;
        }

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        if (leftPaddle != null && rightPaddle != null)
        {
            float halfPaddleHeight = GetHalfHeight(leftPaddle.transform);
            float minY = -halfHeight + padding + halfPaddleHeight;
            float maxY = halfHeight - padding - halfPaddleHeight;

            leftPaddle.SetClamp(minY, maxY);
            rightPaddle.SetClamp(minY, maxY);
        }

        if (topPaddle != null && bottomPaddle != null)
        {
            float halfPaddleWidth = GetHalfWidth(topPaddle.transform);
            float minX = -halfWidth + padding + halfPaddleWidth;
            float maxX = halfWidth - padding - halfPaddleWidth;

            topPaddle.SetClamp(minX, maxX);
            bottomPaddle.SetClamp(minX, maxX);
        }
    }

    private void ApplyNoOverlapClampsPhase2()
    {
        if (leftPaddle == null || rightPaddle == null || topPaddle == null || bottomPaddle == null)
        {
            return;
        }

        float halfLeftWidth = GetHalfWidth(leftPaddle.transform);
        float halfRightWidth = GetHalfWidth(rightPaddle.transform);
        float halfTopWidth = GetHalfWidth(topPaddle.transform);

        float halfLeftHeight = GetHalfHeight(leftPaddle.transform);
        float halfTopHeight = GetHalfHeight(topPaddle.transform);
        float halfBottomHeight = GetHalfHeight(bottomPaddle.transform);

        float minX = leftPaddle.transform.position.x + halfLeftWidth + halfTopWidth;
        float maxX = rightPaddle.transform.position.x - halfRightWidth - halfTopWidth;

        topPaddle.SetClamp(minX, maxX);
        bottomPaddle.SetClamp(minX, maxX);

        float minY = bottomPaddle.transform.position.y + halfBottomHeight + halfLeftHeight;
        float maxY = topPaddle.transform.position.y - halfTopHeight - halfLeftHeight;

        leftPaddle.SetClamp(minY, maxY);
        rightPaddle.SetClamp(minY, maxY);
    }

    private float GetHalfWidth(Transform targetTransform)
    {
        SpriteRenderer spriteRenderer = targetTransform.GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            return spriteRenderer.bounds.extents.x;
        }

        return 0.2f;
    }

    private float GetHalfHeight(Transform targetTransform)
    {
        SpriteRenderer spriteRenderer = targetTransform.GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            return spriteRenderer.bounds.extents.y;
        }

        return 0.2f;
    }

    private void CheckWinner()
    {
        if (player1Score >= pointsToWin)
        {
            EndGame("Player 1 Wins! Press Space to Restart");
            return;
        }

        if (player2Score >= pointsToWin)
        {
            EndGame("Player 2 Wins! Press Space to Restart");
        }
    }

    private void EndGame(string message)
    {
        gameEnded = true;
        SetWinnerText(message);

        if (ball != null)
        {
            ball.StopBall();
        }
    }

    private void UpdateScoreUI()
    {
        if (p1ScoreText != null)
        {
            p1ScoreText.text = player1Score.ToString();
        }

        if (p2ScoreText != null)
        {
            p2ScoreText.text = player2Score.ToString();
        }
    }

    private void SetWinnerText(string message)
    {
        if (winnerText != null)
        {
            winnerText.text = message;
        }
    }
}