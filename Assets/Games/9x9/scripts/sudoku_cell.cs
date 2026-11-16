using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class sudoku_cell : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private GameObject lil_numb_pref;
    [SerializeField] private Transform[] lil_numbs_parents;
    [SerializeField] private GameObject mid_lil_numb_pref;
    [SerializeField] private Transform mid_lil_parent;
    int row, col;
    bool isGiven, waitingForMarker = false;
    public bool isCorrect = false;
    public int currentValue { get; private set; }
    sudoku_base board;

    TMP_Text label;
    public Image background;

    Color normalColor = Color.white;
    Color selectedColor = new Color(0.5f, 0.92f, 1f);
    Color selectedColor1 = new Color(0.55f, 0.99f, 0.55f);
    Color highlightedColor = Color.darkGray;
    Color highlightedColor_lilnumb = Color.gray4;

    // currently selected cell (keyboard target)
    static sudoku_cell s_selected;

    // multiple-cell marker selection (drag with right mouse)
    static HashSet<sudoku_cell> s_markerSelection = new();
    static bool s_markerDragActive = false;
    int? highlightedNumber;

    // expose selection publicly so board/manager can move selection
    public static sudoku_cell Selected
    {
        get => s_selected;
        set
        {
            if (s_selected == value) return;
            var previous = s_selected;
            s_selected = value;
            if (previous != null) previous.SetSelected(false);
            if (s_selected != null) s_selected.SetSelected(true);
        }
    }

    // public getters for row/col and given flag so movement logic can query
    public int Row => row;
    public int Col => col;
    public bool IsGiven => isGiven;

    // current pencil markers for this cell (if any)
    public Dictionary<int, (GameObject, Image)> cornerMarkers = new();
    public Dictionary<int, (GameObject, Image)> middleMarkers = new();    

    public void Initialize(sudoku_base boardRef, int r, int c, int value, bool given, TMP_Text labelRef)
    {
        board = boardRef;
        row = r;
        col = c;
        isGiven = given;
        label = labelRef;

        currentValue = value;
        UpdateVisuals();
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
        s_markerSelection.Remove(this);
        waitingForMarker = false;
        if (s_markerSelection.Count == 0) s_markerDragActive = false;
        if (s_selected == this) Selected = null;
        InputActionsManager.NumberPressed -= HandleDigitEntry;
        InputActionsManager.ErasePressed -= OnErase;
        InputActionsManager.PausePressed -= OnPause;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Left-click selecting cell
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            ClearMarkerSelection();
            Selected = Selected == this ? null : this;
        }        

        // only update same-value highlights on left-click keyboard selection
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            bool showSame = currentValue != 0 && Selected != null;
            board.ShowAllSameValuesOnGrid(showSame ? currentValue : null);
        }
    }

    public void OnErase()
    {
        // early exit if cannot change for this cell
        if (isGiven || isCorrect) return;

        // If cell is part of the marker selection, erase only pencil markers
        if (s_markerSelection.Contains(this))
        {
            ClearPencilMarkers();
            UpdateVisuals();
            board.RefreshHighlights();
            return;
        }

        // otherwise allow erase for the single Selected cell (clear value and markers)
        if (Selected != this) return;

        currentValue = 0;
        ClearPencilMarkers();
        ApplyChange();
        SetSelected(true); // keep selected
    }
    public void OnPause()
    {
        ClearMarkerSelection();
        if (Selected != this) return;
        Selected = null; // deselect on pause
        Debug.Log("Pause triggered from cell");
    }

    // central handler for digit input; shiftPressed => pencil (marker), otherwise actual value
    void HandleDigitEntry(int number, bool shiftPressed)
    {
        // early exit if cannot change for this cell
        if (isGiven || isCorrect) return;

        bool selectionActive = s_markerSelection.Count > 0;
        if (selectionActive ? !s_markerSelection.Contains(this) : Selected != this) return;

        // If doing pencil/marker entry (shift, waitingForMarker or multi-selection) -> add marker(s)
        if (shiftPressed || waitingForMarker || selectionActive)
        {
            // when multi-selection is active, only cells in the selection should receive the marker
            if (selectionActive)
            {
                if (s_markerSelection.Contains(this))
                    AddPencilMarker(number, shiftPressed);
            }
            else if (Selected == this)
            {
                AddPencilMarker(number, shiftPressed);
            }

            board.ShowAllSameValuesOnGrid(number);
            return;
        }

        // normal entry still requires this cell to be the single selected
        if (Selected != this) return;

        // entering a real value clears any pencil mark
        ClearPencilMarkers();
        currentValue = number;
        ApplyChange();
    }

    // add this cell to the global marker selection
    void AddToMarkerSelection()
    {
        // do not allow given or already-solved cells to become part of marker selection
        if (isGiven || isCorrect) return;

        if (!s_markerSelection.Contains(this))
            s_markerSelection.Add(this);
        waitingForMarker = true;
        // directly set marker visual so we don't rely on Selected logic
        if (background != null) background.color = selectedColor1;

    }

    // clear the global marker selection
    static void ClearMarkerSelection()
    {
        var previous = s_markerSelection.ToList();
        s_markerSelection.Clear();
        foreach (var c in previous)
        {
            c.waitingForMarker = false;
            c.RefreshBackground();
        }
    }

    // remove this cell from the global marker selection and refresh visuals for remaining cells
    void RemoveFromMarkerSelection()
    {
        if (!s_markerSelection.Contains(this)) return;

        s_markerSelection.Remove(this);
        waitingForMarker = false;
        RefreshBackground();

        // ensure remaining selected-marker cells keep the correct marker color
        foreach (var c in s_markerSelection)
        {
            if (c.background != null) c.background.color = selectedColor1;
        }
    }

    // called when pointer enters this cell (used for drag-selection)
    public void OnPointerEnter(PointerEventData eventData)
    {
        // only add while right-button drag is active
        if (!s_markerDragActive) return;

        AddToMarkerSelection();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right) return;

        // begin drag selection
        // force-clear keyboard selection visuals immediately
        if (s_selected != null) { s_selected.SetSelected(false); s_selected = null; }
        board.ShowAllSameValuesOnGrid(null);
        // detect ctrl key for multi-select behavior (supports new Input System)
        bool ctrl = false;
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null) ctrl = kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed;

        s_markerDragActive = true;
        // if ctrl is not held, start a fresh selection for this drag
        if (!ctrl) ClearMarkerSelection();
        else if (s_markerSelection.Contains(this)) { RemoveFromMarkerSelection(); s_markerDragActive = false; return; }       
        
        // add this cell to current drag selection (unless it's given/solved)
        AddToMarkerSelection();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right) return;

        // end drag selection (keep selection until user left-clicks elsewhere)
        s_markerDragActive = false;
    }

    void ApplyChange()
    {
        bool valid = board.ValidateMove(row, col, currentValue);

        board.OnCellValueChanged(row, col, currentValue, valid, this);

        UpdateVisuals();
    }

    // instantiate pencil marker under the appropriate child of lil_numbs_parent
    void AddPencilMarker(int number, bool shifted)
    {
        if (number < 1 || number > 9) return; // invalid input

        // already shown => destroy marker
        if (cornerMarkers.ContainsKey(number))
        {
            Destroy(cornerMarkers[number].Item1);
            cornerMarkers.Remove(number);

            // reorder corners after removing
            int i = 0;
            foreach (var marker in cornerMarkers.OrderBy(x => x.Key))
            {
                marker.Value.Item1.transform.SetParent(lil_numbs_parents[i]);
                marker.Value.Item1.transform.localPosition = Vector3.zero;
                i++;
            }
            return;
        }
        if (middleMarkers.ContainsKey(number))
        {
            Destroy(middleMarkers[number].Item1);
            middleMarkers.Remove(number);
            // no need for ordering here
            return;
        }

        // common vars
        Transform target_parent = null;
        GameObject markerType = null;

        if (shifted) MiddleMarkerAdd();
        else CornerMarkerAdd();

        void MiddleMarkerAdd()
        {
            if (mid_lil_parent.childCount > 2) return; // early exit -> no slots available
            target_parent = mid_lil_parent;
            markerType = mid_lil_numb_pref;

            CreateMarker();

            // reorder middles after adding
            int i = 0;
            foreach (var marker in middleMarkers.OrderBy(x => x.Key))
            { marker.Value.Item1.transform.SetSiblingIndex(i); i++; }
        }
        void CornerMarkerAdd()
        {
            if (lil_numbs_parents.All(x => x.childCount > 0)) return; // early exit -> no slots available
            target_parent = lil_numbs_parents.Where(x => x.childCount == 0).First();
            markerType = lil_numb_pref;

            CreateMarker();

            // reorder corners after adding
            int i = 0;
            foreach (var marker in cornerMarkers.OrderBy(x => x.Key))
            {
                marker.Value.Item1.transform.SetParent(lil_numbs_parents[i]);
                marker.Value.Item1.transform.localPosition = Vector3.zero;
                i++;
            }
        }

        void CreateMarker()
        {
            // create new marker and track it
            var currentMarker = Instantiate(markerType, target_parent);
            currentMarker.transform.localPosition = Vector3.zero;
            var img = currentMarker.GetComponentInChildren<Image>();
            if (img != null)
            {
                img.color = selectedColor;
                // allow pointer events to pass through markers so cell receives clicks reliably
                img.raycastTarget = false;
            }

            if (shifted)
                middleMarkers.Add(number, (currentMarker, img));
            else
                cornerMarkers.Add(number, (currentMarker, img));

            // set marker text if TMP present
            var txt = currentMarker.GetComponentInChildren<TMP_Text>();
            if (txt != null) txt.text = number.ToString();
            // ensure text doesn't block pointer events either
            var txtComp = currentMarker.GetComponentInChildren<TMP_Text>();
            if (txtComp != null) txtComp.raycastTarget = false;
        }
    }

    void ClearPencilMarkers()
    {
        foreach (var marker in cornerMarkers.Concat(middleMarkers))
            Destroy(marker.Value.Item1);

        cornerMarkers.Clear();
        middleMarkers.Clear();
    }

    public void SetSelected(bool sel)
    {
        RefreshBackground();
    }

    public void HighlightNumber(int? number)
    {
        highlightedNumber = number;
        foreach (var marker in cornerMarkers.Concat(middleMarkers))
        {
            var image = marker.Value.Item2;
            if (image == null) continue;
            image.enabled = marker.Key == number;
            image.color = highlightedColor_lilnumb;
        }

        RefreshBackground();
    }

    void RefreshBackground()
    {
        if (background == null) return;
        bool matchingMarker = highlightedNumber.HasValue &&
            (cornerMarkers.ContainsKey(highlightedNumber.Value) || middleMarkers.ContainsKey(highlightedNumber.Value));

        // Selection takes priority over search highlights. Rendering never changes input mode.
        if (s_markerSelection.Contains(this)) background.color = selectedColor1;
        else if (Selected == this) background.color = selectedColor;
        else if (matchingMarker) background.color = selectedColor1;
        else if (highlightedNumber.HasValue && currentValue == highlightedNumber.Value) background.color = highlightedColor;
        else background.color = normalColor;
    }

    void UpdateVisuals()
    {
        if (label != null)
        {
            label.text = currentValue == 0 ? "" : currentValue.ToString();
            label.fontStyle = TMPro.FontStyles.Bold;

            if (isGiven) label.color = Color.black;
            else label.color = isCorrect ? Color.blue : Color.red;
        }
    }
}
