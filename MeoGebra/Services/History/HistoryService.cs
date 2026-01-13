using System.Collections.Generic;
using MeoGebra.Models;

namespace MeoGebra.Services.History;

public sealed class HistoryService {
    private readonly Stack<IHistoryCommand> _undo = new();
    private readonly Stack<IHistoryCommand> _redo = new();
    private readonly List<HistorySnapshot> _snapshots = new();
    private int _commandCountSinceSnapshot;
    private readonly int _snapshotInterval;

    public HistoryService(int snapshotInterval = 50) {
        _snapshotInterval = snapshotInterval;
    }

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    public void Execute(IHistoryCommand command, Document document) {
        command.Execute();
        _undo.Push(command);
        _redo.Clear();
        _commandCountSinceSnapshot++;
        if (_commandCountSinceSnapshot >= _snapshotInterval) {
            _snapshots.Add(new HistorySnapshot(document.Clone(), _undo.Count));
            _commandCountSinceSnapshot = 0;
        }
    }

    public void Undo() {
        if (_undo.TryPop(out var command)) {
            command.Undo();
            _redo.Push(command);
        }
    }

    public void Redo() {
        if (_redo.TryPop(out var command)) {
            command.Execute();
            _undo.Push(command);
        }
    }
}

public sealed record HistorySnapshot(Document Document, int CommandIndex);
