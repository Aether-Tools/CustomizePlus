using CustomizePlus.Core.Data;
using CustomizePlus.Templates;
using System.Numerics;
using Xunit;

namespace CustomizePlus.Tests.Templates;

public class TemplateEditorHistoryTests
{
    [Fact]
    public void EndEdit_RecordsOneUndoStepForTheWholeInteraction()
    {
        var history = new TemplateEditorHistory();
        var before = Snapshot(("j_kao", Transform(translationX: 1)));
        var after = Snapshot(("j_kao", Transform(translationX: 4)));

        history.BeginEdit(before);
        history.EndEdit(after);

        Assert.True(history.CanUndo);
        Assert.True(history.TryUndo(after, out var restored));
        Assert.Equal(before, restored);
        Assert.False(history.CanUndo);
        Assert.True(history.CanRedo);
    }

    [Fact]
    public void EndEdit_NoOpClearsPendingEditWithoutAddingUndoStep()
    {
        var history = new TemplateEditorHistory();
        var state = Snapshot(("j_kao", Transform(translationX: 1)));

        history.BeginEdit(state);
        history.EndEdit(state);

        Assert.False(history.HasPendingEdit);
        Assert.False(history.CanUndo);
    }

    [Fact]
    public void BeginEdit_CommitsAnInterruptedEditBeforeStartingTheNextOne()
    {
        var history = new TemplateEditorHistory();
        var initial = Snapshot(("j_kao", Transform(translationX: 1)));
        var afterFirstEdit = Snapshot(("j_kao", Transform(translationX: 2)));
        var afterSecondEdit = Snapshot(("j_kao", Transform(translationX: 3)));

        history.BeginEdit(initial);
        history.BeginEdit(afterFirstEdit);
        history.EndEdit(afterSecondEdit);

        Assert.True(history.TryUndo(afterSecondEdit, out var firstUndo));
        Assert.Equal(afterFirstEdit, firstUndo);
        Assert.True(history.TryUndo(firstUndo, out var secondUndo));
        Assert.Equal(initial, secondUndo);
    }

    [Fact]
    public void RecordEdit_AfterUndoClearsRedoStack()
    {
        var history = new TemplateEditorHistory();
        var initial = Snapshot(("j_kao", Transform(translationX: 1)));
        var edited = Snapshot(("j_kao", Transform(translationX: 2)));
        var replacement = Snapshot(("j_kao", Transform(translationX: 5)));

        history.RecordEdit(initial, edited);
        Assert.True(history.TryUndo(edited, out var restored));
        Assert.Equal(initial, restored);
        Assert.True(history.CanRedo);

        history.RecordEdit(restored, replacement);

        Assert.False(history.CanRedo);
    }

    [Fact]
    public void Reset_ClearsUndoRedoAndPendingEdit()
    {
        var history = new TemplateEditorHistory();
        var initial = Snapshot(("j_kao", Transform(translationX: 1)));
        var edited = Snapshot(("j_kao", Transform(translationX: 2)));

        history.RecordEdit(initial, edited);
        Assert.True(history.TryUndo(edited, out _));
        history.BeginEdit(initial);

        history.Reset();

        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
        Assert.False(history.HasPendingEdit);
    }

    [Fact]
    public void SnapshotEquality_IsStructuralAndIndependentOfDictionaryOrder()
    {
        var first = Snapshot(
            ("j_kao", Transform(translationX: 1)),
            ("j_ago", Transform(scaleY: 2)));
        var second = Snapshot(
            ("j_ago", Transform(scaleY: 2)),
            ("j_kao", Transform(translationX: 1)));

        Assert.Equal(first, second);
    }

    [Fact]
    public void Snapshot_DoesNotChangeWhenSourceTransformsAreMutated()
    {
        var transform = Transform(translationX: 1);
        var source = new Dictionary<string, BoneTransform> { ["j_kao"] = transform };
        var snapshot = TemplateEditorSnapshot.Capture(source);

        transform.Translation = new Vector3(7, 0, 0);

        Assert.Equal(1, snapshot.Bones["j_kao"].Translation.X);
    }

    private static TemplateEditorSnapshot Snapshot(params (string Name, BoneTransform Transform)[] bones)
        => TemplateEditorSnapshot.Capture(bones.ToDictionary(x => x.Name, x => x.Transform));

    private static BoneTransform Transform(float translationX = 0, float scaleY = 1)
        => new()
        {
            Translation = new Vector3(translationX, 0, 0),
            Scaling = new Vector3(1, scaleY, 1),
        };
}
