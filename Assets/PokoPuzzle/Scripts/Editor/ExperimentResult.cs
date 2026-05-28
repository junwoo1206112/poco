#if UNITY_EDITOR
namespace PokoPuzzle.Editor
{
    public sealed class ExperimentResult
    {
        public string Name { get; }
        public string Focus { get; }
        public string LogPath { get; }
        public PlayLogAnalysis Analysis { get; }

        public ExperimentResult(string name, string focus, string logPath, PlayLogAnalysis analysis)
        {
            Name = name;
            Focus = focus;
            LogPath = logPath;
            Analysis = analysis;
        }
    }
}
#endif
