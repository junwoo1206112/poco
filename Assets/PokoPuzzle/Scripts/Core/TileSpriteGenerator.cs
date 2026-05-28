using UnityEngine;

namespace PokoPuzzle.Core
{
    public static class TileSpriteGenerator
    {
        public static Sprite[] CreateTileSprites(PokoTileVisualStyle visualStyle)
        {
            var count = System.Enum.GetValues(typeof(PokoTileType)).Length;
            var sprites = new Sprite[count];
            for (var index = 0; index < count; index++)
            {
                sprites[index] = CreateShapeSprite((PokoTileType)index, visualStyle);
            }

            return sprites;
        }

        private static Sprite CreateShapeSprite(PokoTileType type, PokoTileVisualStyle visualStyle)
        {
            const int size = 96;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            var outerRadius = size * 0.42f;
            var innerRadius = size * 0.34f;
            var outer = new Vector2[6];
            var inner = new Vector2[6];

            for (var index = 0; index < 6; index++)
            {
                var angle = Mathf.Deg2Rad * (60f * index + 30f);
                var cos = Mathf.Cos(angle);
                var sin = Mathf.Sin(angle);
                outer[index] = new Vector2(center.x + outerRadius * cos, center.y + outerRadius * sin);
                inner[index] = new Vector2(center.x + innerRadius * cos, center.y + innerRadius * sin);
            }

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var point = new Vector2(x, y);
                    var inOuter = IsInsideHex(point, outer);
                    var inInner = IsInsideHex(point, inner);

                    if (!inOuter)
                    {
                        texture.SetPixel(x, y, new Color(0f, 0f, 0f, 0f));
                        continue;
                    }

                    if (!inInner)
                    {
                        texture.SetPixel(x, y, new Color(0.35f, 0.35f, 0.35f, 1f));
                        continue;
                    }

                    var inShape = IsInsideShape(point, center, size, type);
                    var gray = inShape ? 0.95f : 0.50f;
                    texture.SetPixel(x, y, new Color(gray, gray, gray, 1f));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static bool IsInsideShape(Vector2 point, Vector2 center, int size, PokoTileType type)
        {
            var dx = point.x - center.x;
            var dy = point.y - center.y;
            var s = size * 0.30f;
            var lw = size * 0.055f;

            switch (type)
            {
                case PokoTileType.Red:
                {
                    var outer = s;
                    var inner = s - lw;
                    var dist = Mathf.Sqrt(dx * dx + dy * dy);
                    return dist <= outer && dist >= inner;
                }

                case PokoTileType.Yellow:
                {
                    var inOuter = Mathf.Abs(dx) <= s && Mathf.Abs(dy) <= s;
                    var inner2 = s - lw;
                    var inInner = Mathf.Abs(dx) <= inner2 && Mathf.Abs(dy) <= inner2;
                    return inOuter && !inInner;
                }

                case PokoTileType.Green:
                {
                    var h = s * 1.4f;
                    var w = s * 1.1f;
                    var y0 = center.y - h * 0.5f;
                    var relY = (point.y - y0) / h;
                    var halfW = w * (1f - relY);
                    var inOuter = relY >= 0f && relY <= 1f && Mathf.Abs(dx) <= halfW;

                    var y1 = y0 + lw;
                    var h2 = h - lw * 2f;
                    var relY2 = h2 > 0f ? (point.y - y1) / h2 : -1f;
                    var halfW2 = (w - lw) * (1f - relY2);
                    var inInner = relY2 >= 0f && relY2 <= 1f && Mathf.Abs(dx) <= halfW2;

                    return inOuter && !inInner;
                }

                case PokoTileType.Blue:
                {
                    var inOuter = Mathf.Abs(dx) + Mathf.Abs(dy) <= s;
                    var inner2 = s - lw;
                    var inInner = Mathf.Abs(dx) + Mathf.Abs(dy) <= inner2;
                    return inOuter && !inInner;
                }

                case PokoTileType.Purple:
                {
                    var outerR = s;
                    var innerR = s * 0.30f;
                    var angle = Mathf.Atan2(dy, dx);
                    var dist = Mathf.Sqrt(dx * dx + dy * dy);
                    var spike = Mathf.Max(0f, Mathf.Cos(angle * 5f));
                    var maxR = innerR + (outerR - innerR) * spike;
                    var minR = Mathf.Max(0f, maxR - lw);
                    return dist <= maxR && dist >= minR;
                }

                case PokoTileType.Orange:
                {
                    var barW = s * 0.25f;
                    var barL = s * 0.80f;
                    var inOuter = (Mathf.Abs(dx) <= barW && Mathf.Abs(dy) <= barL) || (Mathf.Abs(dy) <= barW && Mathf.Abs(dx) <= barL);
                    var iw = Mathf.Max(0f, barW - lw * 0.5f);
                    var il = Mathf.Max(0f, barL - lw);
                    var inInner = (Mathf.Abs(dx) <= iw && Mathf.Abs(dy) <= il) || (Mathf.Abs(dy) <= iw && Mathf.Abs(dx) <= il);
                    return inOuter && !inInner;
                }

                default:
                {
                    var dist = Mathf.Sqrt(dx * dx + dy * dy);
                    return dist <= s && dist >= s - lw;
                }
            }
        }

        private static bool IsInsideHex(Vector2 point, Vector2[] vertices)
        {
            for (var index = 0; index < 6; index++)
            {
                var next = (index + 1) % 6;
                var edge = vertices[next] - vertices[index];
                var toPoint = point - vertices[index];
                var cross = edge.x * toPoint.y - edge.y * toPoint.x;
                if (cross < 0f)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
