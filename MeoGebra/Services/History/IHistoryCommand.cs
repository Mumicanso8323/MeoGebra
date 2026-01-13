namespace MeoGebra.Services.History;

public interface IHistoryCommand {
    string Description { get; }
    void Execute();
    void Undo();
}
