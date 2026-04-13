using System.Collections;
using UnityEngine;

public class Ball : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 8f;
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

    [Header("Audio")]
    [SerializeField] private AudioSource hitSource;
    [SerializeField] private AudioClip hitSfx;
    [SerializeField] private float pitchMin = 1f;
    [SerializeField] private float pitchMax = 1.35f;

    private Vector2 direction;
    private bool isMoving;
    private bool fourPaddlesMode;
    private Coroutine launchCoroutine;
    private float queuedXDirection;

    private void Start()
    {
        ApplyBoundsFromCamera(Camera.main, 0.05f);
        speed = baseSpeed;
        BeginLaunch();
    }

    private void Update()
    {
        if (gameManager != null && gameManager.IsGameEnded)
        {
            return;
        }

        if (!isMoving)
        {
            return;
        }

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

        if (xDirection == 0f)
        {
            Invoke("BeginLaunch", seconds);
        }
        else
        {
            Invoke("LaunchToDirection", seconds);
        }
    }

    public void EnableFourPaddlesMode()
    {
        fourPaddlesMode = true;
    }

    public void DisableFourPaddlesMode()
    {
        fourPaddlesMode = false;
    }

    public void CancelPendingLaunch()
    {
        CancelInvoke();
    }

    public void ApplyBoundsFromCamera(Camera cam, float padding)
    {
        if (cam == null)
        {
            return;
        }

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        minY = -halfHeight + padding;
        maxY = halfHeight - padding;
        minX = -halfWidth + padding;
        maxX = halfWidth - padding;
    }

    private void Move()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    private void HandleWallCollisions()
    {
        if (fourPaddlesMode)
        {
            return;
        }

        Vector3 position = transform.position;
        float halfBallY = ballSize.y * 0.5f;

        if (position.y + halfBallY >= maxY)
        {
            position.y = maxY - halfBallY;
            direction.y *= -1f;
            transform.position = position;
        }
        else if (position.y - halfBallY <= minY)
        {
            position.y = minY + halfBallY;
            direction.y *= -1f;
            transform.position = position;
        }
    }

    private void HandlePaddleCollisions()
    {
        if (CheckVerticalPaddleCollision(leftPaddle, true))
        {
            return;
        }

        if (CheckVerticalPaddleCollision(rightPaddle, false))
        {
            return;
        }

        if (!fourPaddlesMode)
        {
            return;
        }

        if (CheckHorizontalPaddleCollision(topPaddle, true))
        {
            return;
        }

        CheckHorizontalPaddleCollision(bottomPaddle, false);
    }

    private bool CheckVerticalPaddleCollision(Paddle paddle, bool isLeftPaddle)
    {
        if (paddle == null)
        {
            return false;
        }

        if (isLeftPaddle && direction.x >= 0f)
        {
            return false;
        }

        if (!isLeftPaddle && direction.x <= 0f)
        {
            return false;
        }

        Vector3 paddlePosition = paddle.transform.position;
        Vector3 ballPosition = transform.position;

        bool overlapX =
            ballPosition.x + ballSize.x * 0.5f > paddlePosition.x - paddleSize.x * 0.5f &&
            ballPosition.x - ballSize.x * 0.5f < paddlePosition.x + paddleSize.x * 0.5f;

        bool overlapY =
            ballPosition.y + ballSize.y * 0.5f > paddlePosition.y - paddleSize.y * 0.5f &&
            ballPosition.y - ballSize.y * 0.5f < paddlePosition.y + paddleSize.y * 0.5f;

        if (!overlapX || !overlapY)
        {
            return false;
        }

        direction.x *= -1f;
        speed *= speedIncreasePerHit;
        ResolveVerticalOverlap(paddlePosition);
        PlayHitSfx();
        return true;
    }

    private bool CheckHorizontalPaddleCollision(Paddle paddle, bool isTopPaddle)
    {
        if (paddle == null)
        {
            return false;
        }

        if (isTopPaddle && direction.y <= 0f)
        {
            return false;
        }

        if (!isTopPaddle && direction.y >= 0f)
        {
            return false;
        }

        Vector3 paddlePosition = paddle.transform.position;
        Vector3 ballPosition = transform.position;

        bool overlapX =
            ballPosition.x + ballSize.x * 0.5f > paddlePosition.x - horizontalPaddleSize.x * 0.5f &&
            ballPosition.x - ballSize.x * 0.5f < paddlePosition.x + horizontalPaddleSize.x * 0.5f;

        bool overlapY =
            ballPosition.y + ballSize.y * 0.5f > paddlePosition.y - horizontalPaddleSize.y * 0.5f &&
            ballPosition.y - ballSize.y * 0.5f < paddlePosition.y + horizontalPaddleSize.y * 0.5f;

        if (!overlapX || !overlapY)
        {
            return false;
        }

        direction.y *= -1f;
        speed *= speedIncreasePerHit;
        ResolveHorizontalOverlap(paddlePosition);
        PlayHitSfx();
        return true;
    }

    private void ResolveVerticalOverlap(Vector3 paddlePosition)
    {
        float offset = paddleSize.x * 0.5f + ballSize.x * 0.5f + collisionSkin;
        Vector3 position = transform.position;

        if (position.x < paddlePosition.x)
        {
            position.x = paddlePosition.x - offset;
        }
        else
        {
            position.x = paddlePosition.x + offset;
        }

        transform.position = position;
    }

    private void ResolveHorizontalOverlap(Vector3 paddlePosition)
    {
        float offset = horizontalPaddleSize.y * 0.5f + ballSize.y * 0.5f + collisionSkin;
        Vector3 position = transform.position;

        if (position.y < paddlePosition.y)
        {
            position.y = paddlePosition.y - offset;
        }
        else
        {
            position.y = paddlePosition.y + offset;
        }

        transform.position = position;
    }

    private void CheckGoal()
    {
        if (gameManager == null)
        {
            return;
        }

        float ballX = transform.position.x;
        float ballY = transform.position.y;
        float halfBallY = ballSize.y * 0.5f;

        if (ballX < minX)
        {
            ScorePlayer2();
            return;
        }

        if (ballX > maxX)
        {
            ScorePlayer1();
            return;
        }

        if (!fourPaddlesMode)
        {
            return;
        }

        if (ballY + halfBallY > maxY)
        {
            ScorePlayer2();
            return;
        }

        if (ballY - halfBallY < minY)
        {
            ScorePlayer1();
        }
    }

    private void ScorePlayer1()
    {
        gameManager.AddPointToPlayer1();
        ResetBallToCenter();

        if (!gameManager.IsGameEnded)
        {
            LaunchAfterPoint(-1f);
        }
    }

    private void ScorePlayer2()
    {
        gameManager.AddPointToPlayer2();
        ResetBallToCenter();

        if (!gameManager.IsGameEnded)
        {
            LaunchAfterPoint(1f);
        }
    }

    private void LaunchAfterPoint(float serveDirection)
    {
        if (gameManager.IsPhase2Transitioning)
        {
            gameManager.QueueServe(serveDirection);
        }
        else
        {
            BeginLaunch(serveDirection);
        }
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
        float x;
        float y;

        if (xDirection == 0f)
        {
            x = Random.value < 0.5f ? -1f : 1f;
        }
        else
        {
            x = Mathf.Sign(xDirection);
        }

        y = Random.value < 0.5f ? -1f : 1f;

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
        if (hitSource == null || hitSfx == null)
        {
            return;
        }

        float t = Mathf.InverseLerp(baseSpeed, baseSpeed * 2f, speed);
        hitSource.pitch = Mathf.Lerp(pitchMin, pitchMax, t);
        hitSource.PlayOneShot(hitSfx);
    }
}