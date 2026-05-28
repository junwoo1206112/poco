#if UNITY_EDITOR
using System;

namespace PokoPuzzle.Editor
{
    [Serializable]
    public sealed class ResponsesEnvelope
    {
        public ResponsesOutputItem[] output = Array.Empty<ResponsesOutputItem>();
    }

    [Serializable]
    public sealed class ResponsesOutputItem
    {
        public ResponsesContentItem[] content = Array.Empty<ResponsesContentItem>();
    }

    [Serializable]
    public sealed class ResponsesContentItem
    {
        public string type = string.Empty;
        public string text = string.Empty;
    }
}
#endif
