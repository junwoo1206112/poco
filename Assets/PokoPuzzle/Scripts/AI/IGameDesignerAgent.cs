namespace PokoPuzzle.AI
{
    public interface IGameDesignerAgent
    {
        AgentSuggestion Analyze(BoardTelemetry telemetry);
    }
}
