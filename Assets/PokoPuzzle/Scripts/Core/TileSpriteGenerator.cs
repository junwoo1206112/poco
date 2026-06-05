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

            if (visualStyle == PokoTileVisualStyle.CircleInHex)
            {
                FillCircleStyle(texture, size, center, type);
            }
            else
            {
                FillHexStyle(texture, size, center, type);
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static void FillCircleStyle(Texture2D texture, int size, Vector2 center, PokoTileType type)
        {
            var outerRadius = size * 0.45f;
            var innerRadius = size * 0.37f;
            var baseColor = TileColor(type);
            var rimDark = Color.Lerp(baseColor, Color.black, 0.46f);
            var rimLight = Color.Lerp(baseColor, Color.white, 0.28f);

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var point = new Vector2(x, y);
                    var dist = Vector2.Distance(point, center);

                    if (dist > outerRadius)
                    {
                        texture.SetPixel(x, y, new Color(0f, 0f, 0f, 0f));
                        continue;
                    }

                    var highlight = Mathf.Clamp01((point.y - center.y + size * 0.28f) / (size * 0.56f));

                    if (dist > innerRadius)
                    {
                        var ringT = (dist - innerRadius) / (outerRadius - innerRadius);
                        var rimColor = Color.Lerp(rimLight, rimDark, ringT);
                        texture.SetPixel(x, y, new Color(rimColor.r, rimColor.g, rimColor.b, 1f));
                    }
                    else
                    {
                        var inShape = IsInsideShape(point, center, size, type);
                        var edgeShade = Mathf.Clamp01(dist / innerRadius);
                        var face = Color.Lerp(Color.Lerp(baseColor, Color.black, 0.22f), Color.Lerp(baseColor, Color.white, 0.2f), highlight);
                        face = Color.Lerp(face, Color.Lerp(baseColor, Color.black, 0.32f), Mathf.Clamp01((edgeShade - 0.76f) / 0.24f));

                        if (inShape)
                        {
                            var mark = Color.Lerp(Color.white, baseColor, 0.08f);
                            texture.SetPixel(x, y, new Color(mark.r, mark.g, mark.b, 1f));
                            continue;
                        }

                        texture.SetPixel(x, y, new Color(face.r, face.g, face.b, 1f));
                    }
                }
            }
        }

        private static void FillHexStyle(Texture2D texture, int size, Vector2 center, PokoTileType type)
        {
            var outerRadius = size * 0.44f;
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

            var baseColor = TileColor(type);

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
                        var outerEdge = DistanceToHexEdge(point, outer) < 2f;
                        var innerEdge = DistanceToHexEdge(point, inner) < 2f;
                        if (outerEdge || innerEdge)
                        {
                            texture.SetPixel(x, y, new Color(1f, 1f, 1f, 1f));
                        }
                        else
                        {
                            texture.SetPixel(x, y, new Color(0.92f, 0.92f, 0.92f, 1f));
                        }
                        continue;
                    }

                    var inShape = IsInsideShape(point, center, size, type);
                    var highlight = Mathf.Clamp01((point.y - center.y + size * 0.24f) / (size * 0.48f));
                    var faceBright = inShape ? Mathf.Lerp(0.85f, 1f, highlight) : Mathf.Lerp(0.45f, 0.6f, highlight);
                    var innerColor = new Color(
                        baseColor.r * faceBright,
                        baseColor.g * faceBright,
                        baseColor.b * faceBright, 1f);
                    texture.SetPixel(x, y, innerColor);
                }
            }
        }

        private static Color TileColor(PokoTileType type)
        {
            return type switch
            {
                PokoTileType.Red => new Color(0.95f, 0.22f, 0.28f),
                PokoTileType.Yellow => new Color(1f, 0.78f, 0.2f),
                PokoTileType.Green => new Color(0.28f, 0.76f, 0.36f),
                PokoTileType.Blue => new Color(0.22f, 0.55f, 0.95f),
                PokoTileType.Purple => new Color(0.58f, 0.34f, 0.9f),
                PokoTileType.Orange => new Color(1f, 0.52f, 0.18f),
                _ => Color.white
            };
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

        private static float DistanceToHexEdge(Vector2 point, Vector2[] vertices)
        {
            var minDistance = float.MaxValue;
            for (var index = 0; index < 6; index++)
            {
                var next = (index + 1) % 6;
                minDistance = Mathf.Min(minDistance, DistanceToSegment(point, vertices[index], vertices[next]));
            }

            return minDistance;
        }

        private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            var segment = b - a;
            var t = Vector2.Dot(point - a, segment) / Mathf.Max(0.0001f, Vector2.Dot(segment, segment));
            t = Mathf.Clamp01(t);
            return Vector2.Distance(point, a + segment * t);
        }
    }
}
