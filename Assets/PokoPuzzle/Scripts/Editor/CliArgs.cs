#if UNITY_EDITOR
using System;
using System.Globalization;
using PokoPuzzle.Core;

namespace PokoPuzzle.Editor
{
    public readonly struct CliArgs
    {
        private readonly string[] args;

        private CliArgs(string[] args)
        {
            this.args = args;
        }

        public static CliArgs Parse(string[] args)
        {
            return new CliArgs(args);
        }

        public string GetString(string name, string fallback)
        {
            var token = $"--{name}";
            for (var index = 0; index < args.Length - 1; index++)
            {
                if (string.Equals(args[index], token, StringComparison.OrdinalIgnoreCase))
                {
                    return args[index + 1];
                }
            }

            return fallback;
        }

        public bool GetBool(string name, bool fallback)
        {
            var value = GetString(name, fallback ? "true" : "false");
            if (bool.TryParse(value, out var parsed))
            {
                return parsed;
            }

            if (string.Equals(value, "1", StringComparison.Ordinal))
            {
                return true;
            }

            if (string.Equals(value, "0", StringComparison.Ordinal))
            {
                return false;
            }

            return fallback;
        }

        public int GetInt(string name, int fallback)
        {
            return int.TryParse(GetString(name, string.Empty), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                ? value
                : fallback;
        }

        public float GetFloat(string name, float fallback)
        {
            return float.TryParse(GetString(name, string.Empty), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                ? value
                : fallback;
        }

        public bool GetLayout(string name, bool fallback)
        {
            var value = GetString(name, fallback ? "hex" : "square");
            if (string.Equals(value, "hex", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(value, "square", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            throw new InvalidOperationException($"Unsupported layout '{value}'. Use 'hex' or 'square'.");
        }

        public PokoTileVisualStyle GetTileVisualStyle(string name, PokoTileVisualStyle fallback)
        {
            var value = GetString(name, TileVisualName(fallback));
            if (string.Equals(value, "hex", StringComparison.OrdinalIgnoreCase))
            {
                return PokoTileVisualStyle.Hex;
            }

            if (string.Equals(value, "circle-in-hex", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "circleInHex", StringComparison.OrdinalIgnoreCase))
            {
                return PokoTileVisualStyle.CircleInHex;
            }

            throw new InvalidOperationException($"Unsupported tile visual '{value}'. Use 'hex' or 'circle-in-hex'.");
        }

        private static string TileVisualName(PokoTileVisualStyle style)
        {
            return style switch
            {
                PokoTileVisualStyle.Hex => "hex",
                PokoTileVisualStyle.CircleInHex => "circle-in-hex",
                _ => "circle-in-hex"
            };
        }
    }
}
#endif
