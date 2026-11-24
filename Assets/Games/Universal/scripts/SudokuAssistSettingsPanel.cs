using UnityEngine;

public class SudokuAssistSettingsPanel : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Toggle highlightPeers;
    [SerializeField] private UnityEngine.UI.Toggle cleanNotes;

    void OnEnable()
    {
        if (highlightPeers != null)
        {
            highlightPeers.SetIsOnWithoutNotify(SudokuAssistSettings.HighlightPeers);
            highlightPeers.onValueChanged.AddListener(SetHighlightPeers);
        }
        if (cleanNotes != null)
        {
            cleanNotes.SetIsOnWithoutNotify(SudokuAssistSettings.CleanNotes);
            cleanNotes.onValueChanged.AddListener(SetCleanNotes);
        }
    }
    void OnDisable()
    {
        if (highlightPeers != null) highlightPeers.onValueChanged.RemoveListener(SetHighlightPeers);
        if (cleanNotes != null) cleanNotes.onValueChanged.RemoveListener(SetCleanNotes);
    }
    void SetHighlightPeers(bool value) => SudokuAssistSettings.HighlightPeers = value;
    void SetCleanNotes(bool value) => SudokuAssistSettings.CleanNotes = value;
}
