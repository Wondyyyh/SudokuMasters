using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Play-mode regression checks against the real cell code and marker prefabs.
public static class SudokuControlChecks
{
    [MenuItem("Tools/Sudoku/Run Control Checks", true)]
    static bool CanRun() => EditorApplication.isPlaying;

    [MenuItem("Tools/Sudoku/Run Control Checks")]
    static void Run()
    {
        var root = new GameObject("Sudoku control checks");
        root.SetActive(false); // isolated from live keyboard subscriptions and Start()
        int checks = 0;
        sudoku_cell first = null;
        try
        {
            var board = root.AddComponent<sudoku_base>();
            var a = CreateCell(root, board, 0);
            var b = CreateCell(root, board, 1);
            first = a;
            var marker = new GameObject("Existing marker", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            marker.transform.SetParent(b.transform);
            b.cornerMarkers.Add(3, (marker, marker.GetComponent<UnityEngine.UI.Image>()));

            board.ShowAllSameValuesOnGrid(3);
            Check(b.background.color == new Color(0.55f, 0.99f, 0.55f), "Matching pencil marks must be green", ref checks);
            b.OnPointerDown(new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Right });
            board.ShowAllSameValuesOnGrid(null);
            Check(b.background.color == new Color(0.55f, 0.99f, 0.55f), "Right selection must survive highlight reset", ref checks);
            b.OnPointerUp(new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Right });
            b.OnPause();
            Check(b.background.color == Color.white, "Pause must clear marker selection", ref checks);

            sudoku_cell.Selected = a;
            Enter(a, 4, true);
            Enter(b, 4, true); // simulate the broadcast NumberPressed event
            Check(a.middleMarkers.ContainsKey(4) && !b.middleMarkers.ContainsKey(4), "Shift input must target only the selected cell", ref checks);
            Check(a.background.color == new Color(0.5f, 0.92f, 1f), "Single selection must keep its blue background", ref checks);
            sudoku_cell.Selected = null;
            board.ShowAllSameValuesOnGrid(4);
            Check(a.background.color == new Color(0.55f, 0.99f, 0.55f), "Middle marks must also highlight green", ref checks);
            board.ShowAllSameValuesOnGrid(null);
            Check(a.background.color == Color.white, "Clearing number highlight must restore the background", ref checks);
            Enter(b, 5, true);
            Check(!b.middleMarkers.ContainsKey(5), "Shift input without selection must do nothing", ref checks);

            board.ShowAllSameValuesOnGrid(3);
            sudoku_cell.Selected = b;
            Enter(b, 7, false);
            Check(b.currentValue == 7 && b.cornerMarkers.Count == 0, "Highlighting must never switch a cell into pencil input mode", ref checks);
            sudoku_cell.Selected = a;
            Enter(a, 4, true);
            Check(!a.middleMarkers.ContainsKey(4), "Repeating a pencil digit must remove it", ref checks);

            a.OnPause();
            CheckInputActions(ref checks);

            Debug.Log($"Sudoku control checks: PASS ({checks} checks).");
        }
        catch (Exception error)
        {
            Debug.LogError($"Sudoku control checks: FAIL after {checks} checks. {error}");
        }
        finally
        {
            if (first != null) first.OnPause();
            sudoku_cell.Selected = null;
            UnityEngine.Object.Destroy(root);
        }
    }

    static sudoku_cell CreateCell(GameObject root, sudoku_base board, int column)
    {
        var go = new GameObject("Test cell", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        go.transform.SetParent(root.transform);
        var cell = go.AddComponent<sudoku_cell>();
        cell.background = go.GetComponent<UnityEngine.UI.Image>();
        cell.Initialize(board, 0, column, 0, false, null);
        var parents = new Transform[9];
        for (int i = 0; i < 9; i++)
        {
            var parent = new GameObject("Corner", typeof(RectTransform));
            parent.transform.SetParent(go.transform);
            parents[i] = parent.transform;
        }
        var middle = new GameObject("Middle", typeof(RectTransform));
        middle.transform.SetParent(go.transform);
        Set(cell, "lil_numbs_parents", parents);
        Set(cell, "mid_lil_parent", middle.transform);
        Set(cell, "lil_numb_pref", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Games/9x9/prefabs/new_lil_numb.prefab"));
        Set(cell, "mid_lil_numb_pref", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Games/9x9/prefabs/mid_lil_numb.prefab"));
        board.cellValues.Add(cell, 0);
        return cell;
    }

    static void Set(object target, string name, object value) => target.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

    static void Enter(sudoku_cell cell, int number, bool shift) => typeof(sudoku_cell)
        .GetMethod("HandleDigitEntry", BindingFlags.Instance | BindingFlags.NonPublic)
        .Invoke(cell, new object[] { number, shift });

    static void CheckInputActions(ref int count)
    {
        var manager = UnityEngine.Object.FindAnyObjectByType<InputActionsManager>();
        Check(manager != null, "Scene must have an active input manager", ref count);
        var actions = (InputAction[])typeof(InputActionsManager).GetField("numbs", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(manager);
        Check(actions[0].enabled, "Number actions must be enabled", ref count);
        var previousKeyboard = Keyboard.current;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        int received = 0, erased = 0, controls = 0, moved = 0;
        bool shifted = false;
        Action<int, bool> numberHandler = (number, shift) => { received = number; shifted = shift; };
        Action eraseHandler = () => erased++;
        Action ctrlHandler = () => controls++;
        Action<int, int> moveHandler = (row, column) => moved++;
        InputActionsManager.NumberPressed += numberHandler;
        InputActionsManager.ErasePressed += eraseHandler;
        InputActionsManager.CtrlPressed += ctrlHandler;
        InputActionsManager.MovePressed += moveHandler;
        try
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.LeftShift, Key.Digit4));
            InputSystem.Update();
            Check(received == 4 && shifted, $"Shift+digit must be dispatched as pencil input (received={received}, shift={shifted})", ref count);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Delete, Key.RightCtrl, Key.RightArrow));
            InputSystem.Update();
            Check(erased == 1, "Delete must dispatch erase", ref count);
            Check(controls == 1, "Right Ctrl must dispatch Ctrl", ref count);
            Check(moved == 1, "Arrow key must move immediately", ref count);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update();
            manager.enabled = false;
            manager.enabled = true;
            Check(actions[0].enabled, "Input must recover after re-enabling its manager", ref count);
        }
        finally
        {
            InputActionsManager.NumberPressed -= numberHandler;
            InputActionsManager.ErasePressed -= eraseHandler;
            InputActionsManager.CtrlPressed -= ctrlHandler;
            InputActionsManager.MovePressed -= moveHandler;
            InputSystem.RemoveDevice(keyboard);
            if (previousKeyboard != null) previousKeyboard.MakeCurrent();
        }
    }

    static void Check(bool condition, string message, ref int count)
    {
        if (!condition) throw new InvalidOperationException(message);
        count++;
    }
}
