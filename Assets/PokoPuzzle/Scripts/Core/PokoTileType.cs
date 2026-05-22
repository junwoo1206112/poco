using UnityEngine;

namespace PokoPuzzle.Core
{
    public enum PokoTileType
    {
        Red,
        Yellow,
        Green,
        Blue,
        Purple,
        Orange
    }

    public static class PokoTileTypePalette
    {
        private static readonly Color[] Colors =
        {
            new Color(0.95f, 0.22f, 0.28f),
            new Color(1.00f, 0.78f, 0.20f),
            new Color(0.28f, 0.76f, 0.36f),
            new Color(0.22f, 0.55f, 0.95f),
            new Color(0.58f, 0.34f, 0.90f),
            new Color(1.00f, 0.52f, 0.18f)
        };

        public static Color ToColor(this PokoTileType type)
        {
            return Colors[(int)type % Colors.Length];
        }
    }
}
