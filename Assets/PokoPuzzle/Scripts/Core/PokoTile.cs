using UnityEngine;

namespace PokoPuzzle.Core
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(PolygonCollider2D))]
    public sealed class PokoTile : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private PolygonCollider2D hexCollider;

        private static Sprite rainbowSprite;

        public int Column { get; private set; }
        public int Row { get; private set; }
        public PokoTileType Type { get; private set; }
        public PokoBlockSubtype BlockSubtype { get; private set; }
        public bool IsBomb { get; private set; }
        public BombType BombType { get; private set; }
        public bool IsFrozen => BlockSubtype == PokoBlockSubtype.Frozen;
        public bool IsStone => BlockSubtype == PokoBlockSubtype.Stone;
        public bool IsClock => BlockSubtype == PokoBlockSubtype.Clock;
        public bool IsRainbow => BlockSubtype == PokoBlockSubtype.Rainbow;
        public bool IsLinkable => (BlockSubtype == PokoBlockSubtype.None || BlockSubtype == PokoBlockSubtype.Clock || BlockSubtype == PokoBlockSubtype.Rainbow) && !IsBomb;

        private float bombTimer = -1f;
        private const float BombAutoDetonateTime = 5f;
        private static readonly Color FrozenTint = new Color(0.6f, 0.8f, 1f, 1f);
        private static readonly Color StoneTint = new Color(0.5f, 0.5f, 0.5f, 1f);
        private static readonly Color ClockTint = new Color(0.5f, 1f, 0.5f, 1f);
        private static readonly Color BombGlow = new Color(1f, 1f, 1f, 0.3f);

        public void Initialize(int column, int row, PokoTileType type, Sprite sprite)
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (hexCollider == null)
            {
                hexCollider = GetComponent<PolygonCollider2D>();
            }

            Column = column;
            Row = row;
            Type = type;
            BlockSubtype = PokoBlockSubtype.None;
            IsBomb = false;
            bombTimer = -1f;
            spriteRenderer.sprite = sprite;
            ApplyVisual();
            name = $"Tile_{column}_{row}_{type}";
            SetHexCollider();
        }

        public void ConfigureSubtype(PokoBlockSubtype subtype)
        {
            BlockSubtype = subtype;
            IsBomb = false;
            bombTimer = -1f;

            if (subtype == PokoBlockSubtype.Rainbow)
            {
                spriteRenderer.sprite = GetOrCreateRainbowSprite();
            }

            ApplyVisual();
        }

        public void ConfigureBomb(BombType bombType)
        {
            BlockSubtype = PokoBlockSubtype.None;
            IsBomb = true;
            BombType = bombType;
            bombTimer = BombAutoDetonateTime;
            ApplyBombVisual();
        }

        public bool TickBombTimer(float deltaTime)
        {
            if (!IsBomb || bombTimer < 0f)
            {
                return false;
            }

            bombTimer -= deltaTime;
            if (bombTimer <= 0f)
            {
                return true;
            }

            var flash = bombTimer <= 1f && Mathf.FloorToInt(bombTimer * 4f) % 2 == 0;
            if (flash)
            {
                spriteRenderer.color = Color.white;
            }
            else
            {
                spriteRenderer.color = BombType == BombType.Red
                    ? new Color(1f, 0.2f, 0.2f)
                    : new Color(0.2f, 0.4f, 1f);
            }

            return false;
        }

        private void SetHexCollider()
        {
            var colliderRadius = 0.42f;
            var vertices = new Vector2[6];
            for (var index = 0; index < 6; index++)
            {
                var angle = Mathf.Deg2Rad * (60f * index + 30f);
                vertices[index] = new Vector2(
                    colliderRadius * Mathf.Cos(angle),
                    colliderRadius * Mathf.Sin(angle));
            }
            hexCollider.SetPath(0, vertices);
        }

        public void SetGridPosition(int column, int row, Vector3 worldPosition)
        {
            Column = column;
            Row = row;
            transform.position = worldPosition;
            name = $"Tile_{column}_{row}_{Type}";
        }

        public void SetType(PokoTileType type)
        {
            Type = type;
            ApplyVisual();
            name = $"Tile_{Column}_{Row}_{Type}";
        }

        public void SetTypeWithSprite(PokoTileType type, Sprite sprite)
        {
            Type = type;
            if (spriteRenderer != null && sprite != null)
            {
                spriteRenderer.sprite = sprite;
            }

            ApplyVisual();
            name = $"Tile_{Column}_{Row}_{Type}";
        }

        public void SetSelected(bool selected)
        {
            if (!IsLinkable)
            {
                return;
            }

            transform.localScale = selected ? Vector3.one * 1.18f : Vector3.one;
            spriteRenderer.sortingOrder = selected ? 2 : 0;
            ApplyVisual(selected ? 0.25f : 0f);
        }

        public void SetLinkHint(bool hinted)
        {
            if (!IsLinkable)
            {
                return;
            }

            transform.localScale = hinted ? Vector3.one * 1.08f : Vector3.one;
            spriteRenderer.sortingOrder = hinted ? 1 : 0;
            ApplyVisual(hinted ? 0.18f : 0f);
        }

        private void ApplyVisual(float whiteBlend = 0f)
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (IsRainbow)
            {
                spriteRenderer.color = Color.white;
                spriteRenderer.sortingOrder = whiteBlend > 0f ? 2 : 0;
                return;
            }

            var baseColor = Type.ToColor();
            if (IsBomb)
            {
                return;
            }

            var color = whiteBlend > 0f ? Color.Lerp(baseColor, Color.white, whiteBlend) : baseColor;

            if (IsFrozen)
            {
                color = Color.Lerp(color, FrozenTint, 0.5f);
            }
            else if (IsStone)
            {
                color = Color.Lerp(color, StoneTint, 0.6f);
            }
            else if (IsClock)
            {
                color = Color.Lerp(color, ClockTint, 0.4f);
            }

            spriteRenderer.color = color;
        }

        private void ApplyBombVisual()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            spriteRenderer.sprite = CreateBombSprite();
            spriteRenderer.color = BombType == BombType.Red ? new Color(1f, 0.2f, 0.2f) : new Color(0.2f, 0.4f, 1f);
            spriteRenderer.sortingOrder = 3;
        }

        private static Sprite GetOrCreateRainbowSprite()
        {
            if (rainbowSprite != null)
            {
                return rainbowSprite;
            }

            rainbowSprite = CreateRainbowGradient();
            return rainbowSprite;
        }

        private static Sprite CreateRainbowGradient()
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

            var rainbowColors = new Color[]
            {
                new Color(1f, 0.2f, 0.2f),
                new Color(1f, 0.7f, 0.1f),
                new Color(1f, 0.95f, 0.2f),
                new Color(0.2f, 0.8f, 0.2f),
                new Color(0.2f, 0.4f, 1f),
                new Color(0.6f, 0.2f, 0.8f)
            };

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
                    }
                    else if (inInner)
                    {
                        var t = (float)x / size;
                        var idx = Mathf.FloorToInt(t * (rainbowColors.Length - 1));
                        var frac = t * (rainbowColors.Length - 1) - idx;
                        var color = Color.Lerp(rainbowColors[idx], rainbowColors[Mathf.Min(idx + 1, rainbowColors.Length - 1)], frac);
                        var alpha = 0.85f + 0.15f * Mathf.Sin(x * 0.3f + y * 0.2f);
                        texture.SetPixel(x, y, new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha)));
                    }
                    else
                    {
                        texture.SetPixel(x, y, new Color(0.35f, 0.35f, 0.35f, 1f));
                    }
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Sprite CreateBombSprite()
        {
            const int size = 96;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            var outerRadius = size * 0.43f;
            var innerRadius = size * 0.28f;
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
                    }
                    else if (inInner)
                    {
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, 0.9f));
                    }
                    else
                    {
                        texture.SetPixel(x, y, new Color(0.4f, 0.4f, 0.4f, 1f));
                    }
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
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
