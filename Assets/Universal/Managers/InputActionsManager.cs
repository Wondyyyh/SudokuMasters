using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class InputActionsManager : MonoBehaviour
{
    public static event Action<int, bool> NumberPressed;
    public static event Action ErasePressed;
    public static event Action PausePressed;

    private InputAction numbs;
    private InputAction erase;
    private InputAction pause;

    private void Awake()
    {
        // Create actions before using them
        numbs = new InputAction("NumberKey", InputActionType.Button);
        erase = new InputAction("Erase", InputActionType.Button);
        pause = new InputAction("Pause", InputActionType.Button);

        // Explicitly add bindings for top row and numpad
        for (int i = 1; i <= 9; i++)
        {
            // explicit top-row (digit) and numpad keys
            numbs.AddBinding($"<Keyboard>/{i}");
            numbs.AddBinding($"<Keyboard>/numpad{i}");
        }

        erase.AddBinding("<Keyboard>/backspace");
        pause.AddBinding("<Keyboard>/escape");

        // Subscribe handlers
        numbs.performed += OnNumberKey;
        erase.performed += OnErase;
        pause.performed += OnPause;

        // Enable after setup
        numbs.Enable();
        erase.Enable();
        pause.Enable();
    }

    private void OnDisable()
    {
        // disable to stop receiving events
        numbs?.Disable();
        erase?.Disable();
        pause?.Disable();
    }

    private void OnDestroy()
    {
        // unsubscribe and dispose to avoid leaks
        if (numbs != null)
        {
            numbs.performed -= OnNumberKey;
            numbs.Dispose();
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
}
