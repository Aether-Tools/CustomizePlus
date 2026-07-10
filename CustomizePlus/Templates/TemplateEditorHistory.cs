namespace CustomizePlus.Templates;

internal sealed class TemplateEditorHistory
{
    private readonly Stack<TemplateEditorSnapshot> _undoStack = new();
    private readonly Stack<TemplateEditorSnapshot> _redoStack = new();
    private TemplateEditorSnapshot? _pendingEdit;

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;
    public bool HasPendingEdit => _pendingEdit != null;

    public void BeginEdit(TemplateEditorSnapshot current)
    {
        CompletePendingEdit(current);
        _pendingEdit = current;
    }

    public void EndEdit(TemplateEditorSnapshot current)
        => CompletePendingEdit(current);

    public void RecordEdit(TemplateEditorSnapshot before, TemplateEditorSnapshot after)
    {
        CompletePendingEdit(before);
        RecordCompletedEdit(before, after);
    }

    public bool TryUndo(TemplateEditorSnapshot current, out TemplateEditorSnapshot snapshot)
    {
        CompletePendingEdit(current);
        if (!_undoStack.TryPop(out snapshot!))
            return false;

        _redoStack.Push(current);
        return true;
    }

    public bool TryRedo(TemplateEditorSnapshot current, out TemplateEditorSnapshot snapshot)
    {
        CompletePendingEdit(current);
        if (!_redoStack.TryPop(out snapshot!))
            return false;

        _undoStack.Push(current);
        return true;
    }

    public void Reset()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        _pendingEdit = null;
    }

    private void CompletePendingEdit(TemplateEditorSnapshot current)
    {
        if (_pendingEdit == null)
            return;

        var before = _pendingEdit;
        _pendingEdit = null;
        RecordCompletedEdit(before, current);
    }

    private void RecordCompletedEdit(TemplateEditorSnapshot before, TemplateEditorSnapshot after)
    {
        if (before.Equals(after))
            return;

        _undoStack.Push(before);
        _redoStack.Clear();
    }
}
