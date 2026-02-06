using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

public class sudoku_cell : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private GameObject lil_numb_pref;
    [SerializeField] private GameObject lil_numbs_parent;
    int row, col;
    bool isGiven;
    public bool isCorrect = false;
    public int currentValue { get; private set; }
    sudoku_base board;

    TMP_Text label;
    public Image background;

    Color givenColor = new Color(0.9f, 0.9f, 0.9f);
    Color normalColor = Color.white;
    Color wrongColor = new Color(1f, 0.8f, 0.8f);
    Color correctColor = new Color(0.8f, 1f, 0.8f);
    Color selectedColor = new Color(0.8f, 0.92f, 1f);

    // currently selected cell (keyboard target)
    static sudoku_cell s_selected;

    // expose selection publicly so board/manager can move selection
    public static sudoku_cell Selected
    {
        get => s_selected;
        set
        {
            if (s_selected == value) return;
            if (s_selected != null) s_selected.SetSelected(false);
            s_selected = value;
            if (s_selected != null) s_selected.SetSelected(true);
        }
    }

    // public getters for row/col and given flag so movement logic can query
    public int Row => row;
    public int Col => col;
    public bool IsGiven => isGiven;

    // current pencil markers for this cell (if any)
    Dictionary<int, GameObject> currentMarkers = new();

    public void Initialize(sudoku_base boardRef, int r, int c, int value, bool given, TMP_Text labelRef, Image bg)
    {
        board = boardRef;
        row = r;
        col = c;
        isGiven = given;
        label = labelRef;
        background = bg;

        currentValue = value;
        UpdateVisuals();

        if (isGiven)
        {
            // visually mark given cells
            if (label != null) label.fontStyle = TMPro.FontStyles.Bold;
            if (background != null) background.color = givenColor;

            // ensure no pencil marks remain on given cells
            ClearPencilMarkers();
        }
    }

    void OnEnable()
    {
        // ensure visuals match selection state if re-enabled
        if (s_selected == this) SetSelected(true);
        InputActionsManager.NumberPressed += HandleDigitEntry;
        InputActionsManager.ErasePressed += OnErase;
        InputActionsManager.PausePressed += OnPause;
    }

    void OnDisable()
    {
        if (s_selected == this) Selected = null;
        InputActionsManager.NumberPressed -= HandleDigitEntry;
        InputActionsManager.ErasePressed -= OnErase;
        InputActionsManager.PausePressed -= OnPause;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // select/deselect for keyboard input
        if (eventData.button == PointerEventData.InputButton.Left)
            Selected = Selected == this ? null : this;

        // deselect on right-click
        else if (eventData.button == PointerEventData.InputButton.Right)
            Selected = null;

        bool showSame = currentValue != 0 && Selected != null;
        board.ShowAllSameValuesOnGrid(showSame ? currentValue : null);


    }
    public void OnErase()
    {
        // early exit if cannot change
        if (isGiven || isCorrect || Selected != this) return;

        currentValue = 0;
        ClearPencilMarkers();
        ApplyChange();
        SetSelected(true); // keep selected
    }
    public void OnPause()
    {
        if (Selected != this) return;
        Selected = null; // deselect on pause
        Debug.Log("Pause triggered from cell");
    }

    // central handler for digit input; shiftPressed => pencil (marker), otherwise actual value
    void HandleDigitEntry(int number, bool shiftPressed)
    {
        // early exit if cannot change
        if (isGiven || isCorrect || Selected != this) return;

        if (shiftPressed)
        {
            ShowPencilMarker(number);
        }
        else
        {
            // entering a real value clears any pencil mark
            ClearPencilMarkers();
            currentValue = number;
            ApplyChange();
        }
    }

    void ApplyChange()
    {
        UpdateVisuals();
        bool valid = board.ValidateMove(row, col, currentValue);

        // color feedback (unless selected — selected overlay takes precedence)
        if (background != null && Selected != this)
        {
            if (currentValue == 0) background.color = normalColor;
            else background.color = valid ? correctColor : wrongColor;
        }

        board.OnCellValueChanged(row, col, currentValue, valid, this);
    }

    // instantiate pencil marker under the appropriate child of lil_numbs_parent
    void ShowPencilMarker(int number)
    {
        if (number < 1 || number > 9) return; // invalid
        if (currentMarkers.ContainsKey(number)) return; // already shown

        // instantiate under the specific number placeholder so layout/positioning is correct
        var target = lil_numbs_parent.transform.GetChild(number - 1);
        var currentMarker = Instantiate(lil_numb_pref, target);
        currentMarker.transform.localPosition = Vector3.zero;
        currentMarkers.Add(number, currentMarker);

        // set marker text if TMP present
        var txt = currentMarker.GetComponentInChildren<TMP_Text>();
        if (txt != null) txt.text = number.ToString();
    }

    void ClearPencilMarkers()
    {
        if (currentMarkers.Count > 0)
        {
            foreach (var marker in currentMarkers.Values)
                Destroy(marker);
            currentMarkers.Clear();
        }
    }

    public void SetSelected(bool sel)
    {
        if (background == null) return;

        if (sel) background.color = selectedColor;

        else
        {
            // restore color based on current value/validity or given status
            if (isGiven)
            {
                background.color = givenColor;
            }
            else if (currentValue == 0)
            {
                background.color = normalColor;
            }
            else
            {
                bool valid = board.ValidateMove(row, col, currentValue);
                background.color = valid ? normalColor : wrongColor;
            }
        }
    }

    void UpdateVisuals()
    {
        if (label != null)
        {
            label.text = currentValue == 0 ? "" : currentValue.ToString();
            label.fontStyle = TMPro.FontStyles.Bold;
            label.color = isGiven ? Color.black : Color.blue;
        }

        // keep background consistent with selection or value
        if (background != null)
        {
            if (Selected == this)
            {
                background.color = selectedColor;
            }
            else
            {
                if (isGiven) background.color = givenColor;
                else if (currentValue == 0) background.color = normalColor;
                else
                {
                    bool valid = board.ValidateMove(row, col, currentValue);
                    background.color = valid ? correctColor : wrongColor;
                }
            }
        }
    }
}