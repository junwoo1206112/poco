#if UNITY_EDITOR
namespace PokoPuzzle.Editor
{
    public sealed class ExperimentRecommendation
    {
        public string VariantName { get; }
        public string Reason { get; }
        public string NextStep { get; }

        public ExperimentRecommendation(string variantName, string reason, string nextStep)
        {
            VariantName = variantName;
            Reason = reason;
            NextStep = nextStep;
        }
    }
}
#endif
