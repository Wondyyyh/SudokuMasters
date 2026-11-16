using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class InputActionsManager : MonoBehaviour
{
    public static event Action<int, bool> NumberPressed;
    public static event Action ErasePressed;
    public static event Action PausePressed;
    public static event Action CtrlPressed;

    // Movement: delta row, delta col (row -1 = up, +1 = down; col -1 = left, +1 = right)
    public static event Action<int, int> MovePressed;

    private InputAction[] numbs;
    private InputAction erase;
    private InputAction pause;
    private InputAction move;
    private InputAction ctrl;

    // repeat behavior
    private Coroutine _moveRepeatCoroutine;
    private Vector2 _currentMoveVector = Vector2.zero;
    [SerializeField] private float _moveInitialDelay = 0.35f;
    [SerializeField] private float _moveRepeatRate = 0.08f;

    private void Awake()
    {
        // Create actions before using them
        // create one InputAction per number so simultaneous presses each fire
        numbs = new InputAction[9];
        erase = new InputAction("Erase", InputActionType.Button);
        pause = new InputAction("Pause", InputActionType.Button);

        // Move is a Vector2 value coming from a 2D composite (WASD and arrows)
        move = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
        ctrl = new InputAction("Ctrl", InputActionType.Button);

        // Explicitly add bindings for top row and numpad - each action gets its own bindings
        for (int i = 1; i <= 9; i++)
        {
            int idx = i - 1;
            numbs[idx] = new InputAction($"NumberKey{i}", InputActionType.Button);
            // explicit top-row (digit) and numpad keys
            numbs[idx].AddBinding($"<Keyboard>/{i}");
            numbs[idx].AddBinding($"<Keyboard>/numpad{i}");
        }

        erase.AddBinding("<Keyboard>/backspace");
        erase.AddBinding("<Keyboard>/delete");
        pause.AddBinding("<Keyboard>/escape");
        ctrl.AddBinding("<Keyboard>/leftCtrl");
        ctrl.AddBinding("<Keyboard>/rightCtrl");

        // WASD composite
        move.AddCompositeBinding("2DVector")
            .With("up", "<Keyboard>/w")
            .With("down", "<Keyboard>/s")
            .With("left", "<Keyboard>/a")
            .With("right", "<Keyboard>/d");

        // Arrow keys composite
        move.AddCompositeBinding("2DVector")
            .With("up", "<Keyboard>/upArrow")
            .With("down", "<Keyboard>/downArrow")
            .With("left", "<Keyboard>/leftArrow")
            .With("right", "<Keyboard>/rightArrow");

        // Subscribe handlers
        // Each number action will invoke NumberPressed on performed
        erase.performed += OnErase;
        pause.performed += OnPause;
        ctrl.performed += OnCtrl;
        for (int i = 1; i <= 9; i++)
        {
            int idx = i - 1;
            int number = i; // capture for lambda
            numbs[idx].performed += ctx =>
            {
                if (!ctx.performed) return;
                bool shifted =
                    Keyboard.current?.leftShiftKey?.isPressed == true ||
                    Keyboard.current?.rightShiftKey?.isPressed == true;
                NumberPressed?.Invoke(number, shifted);
            };
        }

        // movement: use started/canceled to support repeat on hold
        move.performed += OnMoveStarted;
        move.canceled += OnMoveCanceled;
    }

    private void OnEnable()
    {
        // Enable after setup
        for (int i = 0; i < numbs.Length; i++)
            numbs[i].Enable();
        erase.Enable();
        pause.Enable();
        ctrl.Enable();
        move.Enable();
    }

    private void OnDisable()
    {
        // disable to stop receiving events
        if (numbs != null)
        {
            for (int i = 0; i < numbs.Length; i++)
                numbs[i]?.Disable();
        }
        erase?.Disable();
        pause?.Disable();
        ctrl?.Disable();
        move?.Disable();

        StopMoveRepeat();
    }

    private void OnDestroy()
    {
        // unsubscribe and dispose to avoid leaks
        if (numbs != null)
        {
            for (int i = 0; i < numbs.Length; i++)
            {
                if (numbs[i] != null)
                {
                    // dispose will remove subscriptions
                    numbs[i].Dispose();
                    numbs[i] = null;
                }
            }
            numbs = null;
        }

        if (erase != null)
        {
            erase.performed -= OnErase;
            erase.Dispose();
            erase = null;
        }

        if (pause != null)
        {
            pause.performed -= OnPause;
            pause.Dispose();
            pause = null;
        }

        if (ctrl != null)
        {
            ctrl.performed -= OnCtrl;
            ctrl.Dispose();
            ctrl = null;
        }

        if (move != null)
        {
            move.performed -= OnMoveStarted;
            move.canceled -= OnMoveCanceled;
            move.Dispose();
            move = null;
        }
    }

    private void OnMoveStarted(InputAction.CallbackContext ctx)
    {
        bool wasMoving = _currentMoveVector != Vector2.zero;
        _currentMoveVector = ctx.ReadValue<Vector2>();
        var dir = GetDirectionFromVector(_currentMoveVector);
        if (dir.dr != 0 || dir.dc != 0)
        {
            // immediate first step
            if (!wasMoving) MovePressed?.Invoke(dir.dr, dir.dc);
            // start repeat coroutine
            if (_moveRepeatCoroutine == null)
                _moveRepeatCoroutine = StartCoroutine(MoveRepeatCoroutine());
        }
    }

    private void OnMoveCanceled(InputAction.CallbackContext ctx)
    {
        _currentMoveVector = Vector2.zero;
        StopMoveRepeat();
    }

    private void StopMoveRepeat()
    {
        if (_moveRepeatCoroutine != null)
        {
            StopCoroutine(_moveRepeatCoroutine);
            _moveRepeatCoroutine = null;
        }
    }

    private IEnumerator MoveRepeatCoroutine()
    {
        // initial delay
        yield return new WaitForSeconds(_moveInitialDelay);

        while (true)
        {
            if (_currentMoveVector == Vector2.zero)
                yield break;

            var dir = GetDirectionFromVector(_currentMoveVector);
            if (dir.dr != 0 || dir.dc != 0)
                MovePressed?.Invoke(dir.dr, dir.dc);

            yield return new WaitForSeconds(_moveRepeatRate);
        }
    }

    // choose primary axis and convert to discrete dr,dc
    private (int dr, int dc) GetDirectionFromVector(Vector2 v)
    {
        int dr = 0, dc = 0;
        if (v == Vector2.zero) return (0, 0);

        // prefer vertical if magnitude higher
        if (Mathf.Abs(v.y) >= Mathf.Abs(v.x))
        {
            if (v.y > 0.5f) dr = -1; // up -> decrease row
            else if (v.y < -0.5f) dr = 1; // down -> increase row
        }
        else
        {
            if (v.x < -0.5f) dc = -1; // left
            else if (v.x > 0.5f) dc = 1; // right
        }

        return (dr, dc);
    }

    public void OnNumberKey(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        var key = context.control as KeyControl;
        if (key == null) return;

        int number = -1;
        if (key.keyCode >= Key.Digit1 && key.keyCode <= Key.Digit9)
            number = key.keyCode - Key.Digit1 + 1;
        else if (key.keyCode >= Key.Numpad1 && key.keyCode <= Key.Numpad9)
            number = key.keyCode - Key.Numpad1 + 1;
        else
            return;

        bool shifted =
            Keyboard.current?.leftShiftKey?.isPressed == true ||
            Keyboard.current?.rightShiftKey?.isPressed == true;

        NumberPressed?.Invoke(number, shifted);
    }

    public void OnErase(InputAction.CallbackContext context)
    {
        if (context.performed)
            ErasePressed?.Invoke();
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        if (context.performed)
            PausePressed?.Invoke();
    }
    public void OnCtrl(InputAction.CallbackContext context)
    {
        if (context.performed)
            CtrlPressed?.Invoke();
    }
}
