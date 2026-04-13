using UnityEngine;
using UnityEngine.InputSystem;

public class PongInputManager : MonoBehaviour, Players.IGameplayActions
{
    [Header("Paddles")]
    [SerializeField] private Paddle leftPaddle;
    [SerializeField] private Paddle rightPaddle;
    [SerializeField] private Paddle topPaddle;
    [SerializeField] private Paddle bottomPaddle;

    [Header("Managers")]
    [SerializeField] private PongPauseManager pauseManager;
    [SerializeField] private MonoBehaviour restartHandlerBehaviour;

    private Players input;
    private IPongRestartHandler restartHandler;

    private void Awake()
    {
        input = new Players();
        input.Gameplay.SetCallbacks(this);

        restartHandler = restartHandlerBehaviour as IPongRestartHandler;

        if (restartHandlerBehaviour != null && restartHandler == null)
        {
            Debug.LogError(restartHandlerBehaviour.name + " no implementa IPongRestartHandler.", this);
        }
    }

    private void OnEnable()
    {
        input.Gameplay.Enable();
    }

    private void OnDisable()
    {
        input.Gameplay.Disable();
    }

    private void OnDestroy()
    {
        input.Dispose();
    }

    public void OnMoveP1(InputAction.CallbackContext context)
    {
        if (leftPaddle != null)
        {
            leftPaddle.SetInput(context.ReadValue<float>());
        }
    }

    public void OnMoveP2(InputAction.CallbackContext context)
    {
        if (rightPaddle != null)
        {
            rightPaddle.SetInput(context.ReadValue<float>());
        }
    }

    public void OnMoveTop(InputAction.CallbackContext context)
    {
        if (topPaddle != null)
        {
            topPaddle.SetInput(context.ReadValue<float>());
        }
    }

    public void OnMoveBottom(InputAction.CallbackContext context)
    {
        if (bottomPaddle != null)
        {
            bottomPaddle.SetInput(context.ReadValue<float>());
        }
    }

    public void OnRestart(InputAction.CallbackContext context)
    {
        if (!context.performed)
        {
            return;
        }

        if (restartHandler != null)
        {
            restartHandler.TryRestartGame();
        }
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        if (!context.performed)
        {
            return;
        }

        if (pauseManager != null)
        {
            pauseManager.TogglePause();
        }
    }
}