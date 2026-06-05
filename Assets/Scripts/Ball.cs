using System.Collections;
using UnityEngine;

public class Ball : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float baseSpeed = 8f;
    [SerializeField] private float launchDelay = 1f;
    [SerializeField] private float speedIncreasePerHit = 1.05f;

    [Header("Bounds")]
    [SerializeField] private float minY = -4.5f;
    [SerializeField] private float maxY = 4.5f;
    [SerializeField] private float minX = -8.5f;
    [SerializeField] private float maxX = 8.5f;

    [Header("References")]
    [SerializeField] private Paddle leftPaddle;
    [SerializeField] private Paddle rightPaddle;
    [SerializeField] private Paddle topPaddle;
    [SerializeField] private Paddle bottomPaddle;
    [SerializeField] private PongGameManagerCore gameManager;

    [Header("Collision Sizes")]
    [SerializeField] private Vector2 paddleSize = new Vector2(0.5f, 3f);
    [SerializeField] private Vector2 horizontalPaddleSize = new Vector2(3f, 0.5f);
    [SerializeField] private Vector2 ballSize = new Vector2(0.25f, 0.25f);
    [SerializeField] private float collisionSkin = 0.02f;

    [Header("Advanced Bounce")]
    [SerializeField] private float bounceAngleStrength = 1.0f;
    [SerializeField] private float randomJitter = 0.03f;
    [SerializeField] private float spinFromPaddleInput = 0.15f;

    [Header("Audio")]
    [SerializeField] private AudioSource hitSource;
    [SerializeField] private AudioClip hitSfx;
    [SerializeField] private float pitchMin = 1f;
    [SerializeField] private float pitchMax = 1.35f;

    private Vector2 direction;
    private float speed;
    private bool isMoving;
    private bool fourPaddlesMode;

    private Coroutine launchCoroutine;
    private float queuedXDirection;

    private void Start()
    {
        speed = baseSpeed;
        ApplyBoundsFromCamera(Camera.main, 0.05f);
        BeginLaunch();
    }

    public void SetSpeedIncreasePerHit(float value) => speedIncreasePerHit = value;

    public void EnableFourPaddlesMode() => fourPaddlesMode = true;
    public void DisableFourPaddlesMode() => fourPaddlesMode = false;

    private void Update()
    {
        if (gameManager != null && gameManager.IsGameEnded) return;
        if (!isMoving) return;

        Move();
        HandleWallCollisions();
        HandlePaddleCollisions();
        CheckGoal();
    }

    public void StopBall()
    {
        isMoving = false;
        StopLaunchRoutine();
        CancelInvoke();
    }

    public void ResetBallToCenter()
    {
        isMoving = false;
        transform.position = Vector3.zero;
        speed = baseSpeed;
        StopLaunchRoutine();
        CancelInvoke();
        ApplyBoundsFromCamera(Camera.main, 0.05f);
    }

    public void CancelPendingLaunch() => CancelInvoke();

    public void BeginLaunch()
    {
        StopLaunchRoutine();
        launchCoroutine = StartCoroutine(LaunchRoutine());
    }

    public void BeginLaunch(float xDirection)
    {
        StopLaunchRoutine();
        launchCoroutine = StartCoroutine(LaunchRoutine(xDirection));
    }

    public void BeginLaunchInstant(float xDirection)
    {
        StopLaunchRoutine();
        CancelInvoke();
        LaunchNow(xDirection);
    }

    public void PauseAndRelaunch(float seconds, float xDirection)
    {
        StopBall();
        queuedXDirection = xDirection;

        if (xDirection == 0f) Invoke(nameof(BeginLaunch), seconds);
        else Invoke(nameof(LaunchToDirection), seconds);
    }

    public void ApplyBoundsFromCamera(Camera cam, float padding)
    {
        if (cam == null) return;

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        minY = -halfH + padding;
        maxY = halfH - padding;
        minX = -halfW + padding;
        maxX = halfW - padding;
    }

    private void Move()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    private void HandleWallCollisions()
    {
        if (fourPaddlesMode) return;

        Vector3 pos = transform.position;
        float halfBallY = ballSize.y * 0.5f;

        if (pos.y + halfBallY >= maxY)
        {
            pos.y = maxY - halfBallY;
            direction.y *= -1f;
            transform.position = pos;
        }
        else if (pos.y - halfBallY <= minY)
        {
            pos.y = minY + halfBallY;
            direction.y *= -1f;
            transform.position = pos;
        }
    }

    private void HandlePaddleCollisions()
    {
        if (CheckVerticalPaddleCollision(leftPaddle, true)) return;
        if (CheckVerticalPaddleCollision(rightPaddle, false)) return;

        if (!fourPaddlesMode) return;

        if (CheckHorizontalPaddleCollision(topPaddle, true)) return;
        CheckHorizontalPaddleCollision(bottomPaddle, false);
    }

    private bool CheckVerticalPaddleCollision(Paddle paddle, bool isLeftPaddle)
    {
        if (paddle == null) return false;
        if (isLeftPaddle && direction.x >= 0f) return false;
        if (!isLeftPaddle && direction.x <= 0f) return false;

        Vector3 paddlePos = paddle.transform.position;
        Vector3 ballPos = transform.position;

        bool overlapX =
            ballPos.x + ballSize.x * 0.5f > paddlePos.x - paddleSize.x * 0.5f &&
            ballPos.x - ballSize.x * 0.5f < paddlePos.x + paddleSize.x * 0.5f;

        bool overlapY =
            ballPos.y + ballSize.y * 0.5f > paddlePos.y - paddleSize.y * 0.5f &&
            ballPos.y - ballSize.y * 0.5f < paddlePos.y + paddleSize.y * 0.5f;

        if (!overlapX || !overlapY) return false;

        float relY = (ballPos.y - paddlePos.y) / (paddleSize.y * 0.5f);
        relY = Mathf.Clamp(relY, -1f, 1f);

        float spin = paddle.MoveInput * spinFromPaddleInput;
        float jitter = Random.Range(-randomJitter, randomJitter);

        float xSign = isLeftPaddle ? 1f : -1f;
        float newY = (relY * bounceAngleStrength) + spin + jitter;

        direction = new Vector2(xSign, newY).normalized;

        speed *= speedIncreasePerHit;
        ResolveVerticalOverlap(paddlePos);
        PlayHitSfx();
        return true;
    }

    private bool CheckHorizontalPaddleCollision(Paddle paddle, bool isTopPaddle)
    {
        if (paddle == null) return false;
        if (isTopPaddle && direction.y <= 0f) return false;
        if (!isTopPaddle && direction.y >= 0f) return false;

        Vector3 paddlePos = paddle.transform.position;
        Vector3 ballPos = transform.position;

        bool overlapX =
            ballPos.x + ballSize.x * 0.5f > paddlePos.x - horizontalPaddleSize.x * 0.5f &&
            ballPos.x - ballSize.x * 0.5f < paddlePos.x + horizontalPaddleSize.x * 0.5f;

        bool overlapY =
            ballPos.y + ballSize.y * 0.5f > paddlePos.y - horizontalPaddleSize.y * 0.5f &&
            ballPos.y - ballSize.y * 0.5f < paddlePos.y + horizontalPaddleSize.y * 0.5f;

        if (!overlapX || !overlapY) return false;

        float relX = (ballPos.x - paddlePos.x) / (horizontalPaddleSize.x * 0.5f);
        relX = Mathf.Clamp(relX, -1f, 1f);

        float spin = paddle.MoveInput * spinFromPaddleInput;
        float jitter = Random.Range(-randomJitter, randomJitter);

        float ySign = isTopPaddle ? -1f : 1f;
        float newX = (relX * bounceAngleStrength) + spin + jitter;

        direction = new Vector2(newX, ySign).normalized;

        speed *= speedIncreasePerHit;
        ResolveHorizontalOverlap(paddlePos);
        PlayHitSfx();
        return true;
    }

    private void ResolveVerticalOverlap(Vector3 paddlePos)
    {
        float offset = paddleSize.x * 0.5f + ballSize.x * 0.5f + collisionSkin;
        Vector3 pos = transform.position;

        pos.x = (pos.x < paddlePos.x) ? (paddlePos.x - offset) : (paddlePos.x + offset);
        transform.position = pos;
    }

    private void ResolveHorizontalOverlap(Vector3 paddlePos)
    {
        float offset = horizontalPaddleSize.y * 0.5f + ballSize.y * 0.5f + collisionSkin;
        Vector3 pos = transform.position;

        pos.y = (pos.y < paddlePos.y) ? (paddlePos.y - offset) : (paddlePos.y + offset);
        transform.position = pos;
    }

    private void CheckGoal()
    {
        if (gameManager == null) return;

        float x = transform.position.x;
        float y = transform.position.y;
        float halfBallY = ballSize.y * 0.5f;

        if (x < minX) { ScorePlayer2(); return; }
        if (x > maxX) { ScorePlayer1(); return; }

        if (!fourPaddlesMode) return;

        if (y + halfBallY > maxY) { ScorePlayer2(); return; }
        if (y - halfBallY < minY) { ScorePlayer1(); }
    }

    private void ScorePlayer1()
    {
        gameManager.AddPointToPlayer1();
        ResetBallToCenter();

        if (gameManager.IsPhase2Transitioning || gameManager.IsGameEnded) return;

        LaunchAfterPoint(-1f);
    }

    private void ScorePlayer2()
    {
        gameManager.AddPointToPlayer2();
        ResetBallToCenter();

        if (gameManager.IsPhase2Transitioning || gameManager.IsGameEnded) return;

        LaunchAfterPoint(1f);
    }

    private void LaunchAfterPoint(float serveDirection)
    {
        if (gameManager.IsPhase2Transitioning)
            gameManager.QueueServe(serveDirection);
        else
            BeginLaunch(serveDirection);
    }

    private IEnumerator LaunchRoutine()
    {
        yield return new WaitForSeconds(launchDelay);
        LaunchNow(0f);
        launchCoroutine = null;
    }

    private IEnumerator LaunchRoutine(float xDirection)
    {
        yield return new WaitForSeconds(launchDelay);
        LaunchNow(xDirection);
        launchCoroutine = null;
    }

    private void LaunchNow(float xDirection)
    {
        float x = (xDirection == 0f) ? (Random.value < 0.5f ? -1f : 1f) : Mathf.Sign(xDirection);
        float y = Random.value < 0.5f ? -1f : 1f;

        direction = new Vector2(x, y).normalized;
        isMoving = true;
    }

    private void StopLaunchRoutine()
    {
        if (launchCoroutine != null)
        {
            StopCoroutine(launchCoroutine);
            launchCoroutine = null;
        }
    }

    private void LaunchToDirection()
    {
        BeginLaunch(queuedXDirection);
    }

    private void PlayHitSfx()
    {
        if (hitSource == null || hitSfx == null) return;

        float t = Mathf.InverseLerp(baseSpeed, baseSpeed * 2f, speed);
        hitSource.pitch = Mathf.Lerp(pitchMin, pitchMax, t);
        hitSource.PlayOneShot(hitSfx);
    }
}