using System.Collections;
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
        private static Sprite statusOverlaySprite;

        public int Column { get; private set; }
        public int Row { get; private set; }
        public PokoTileType Type { get; private set; }
        public PokoBlockSubtype BlockSubtype { get; private set; }
        public bool IsBomb { get; private set; }
        public BombType BombType { get; private set; }
        public bool IsFrozen => BlockSubtype == PokoBlockSubtype.Frozen;
        public bool IsStone => BlockSubtype == PokoBlockSubtype.Stone;
        public bool IsClock => BlockSubtype == PokoBlockSubtype.Clock;
        public bool IsLinkable => (BlockSubtype == PokoBlockSubtype.None || BlockSubtype == PokoBlockSubtype.Clock) && !IsBomb;

        private float bombTimer = -1f;
        private bool selected;
        private bool hinted;
        private bool clearing;
        private SpriteRenderer statusOverlay;
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
            selected = false;
            hinted = false;
            clearing = false;
            bombTimer = -1f;
            if (hexCollider != null)
            {
                hexCollider.enabled = true;
            }

            transform.localScale = Vector3.one;
            spriteRenderer.sprite = sprite;
            EnsureStatusOverlay();
            ApplyVisual();
            name = $"Tile_{column}_{row}_{type}";
            SetHexCollider();
        }

        public void ConfigureSubtype(PokoBlockSubtype subtype)
        {
            BlockSubtype = subtype;
            IsBomb = false;
            selected = false;
            hinted = false;
            clearing = false;
            bombTimer = -1f;
            if (hexCollider != null)
            {
                hexCollider.enabled = true;
            }

            transform.localScale = Vector3.one;
            EnsureStatusOverlay();
            ApplyVisual();
        }

        public void ConfigureBomb(BombType bombType)
        {
            BlockSubtype = PokoBlockSubtype.None;
            IsBomb = true;
            selected = false;
            hinted = false;
            clearing = false;
            BombType = bombType;
            bombTimer = bombType == BombType.Rainbow ? -1f : BombAutoDetonateTime;
            if (hexCollider != null)
            {
                hexCollider.enabled = true;
            }

            transform.localScale = Vector3.one;

            if (bombType == BombType.Rainbow)
            {
                spriteRenderer.sprite = GetOrCreateRainbowSprite();
                spriteRenderer.color = Color.white;
                spriteRenderer.sortingOrder = 3;
                HideStatusOverlay();
            }
            else
            {
                ApplyBombVisual();
            }
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

            if (BombType == BombType.Rainbow)
            {
                var flash = bombTimer <= 1f && Mathf.FloorToInt(bombTimer * 4f) % 2 == 0;
                spriteRenderer.color = flash ? new Color(1f, 1f, 1f, 0.5f) : Color.white;
                spriteRenderer.sortingOrder = flash ? 4 : 3;
                return false;
            }

            var flash2 = bombTimer <= 1f && Mathf.FloorToInt(bombTimer * 4f) % 2 == 0;
            if (flash2)
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

        public void PlayClearAndDestroy(float duration = 0.18f)
        {
            if (clearing)
            {
                return;
            }

            clearing = true;
            selected = false;
            hinted = false;
            if (hexCollider != null)
            {
                hexCollider.enabled = false;
            }

            StartCoroutine(AnimateClearAndDestroy(duration));
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
            if (!IsLinkable || clearing)
            {
                return;
            }

            this.selected = selected;
            if (!selected)
            {
                hinted = false;
            }

            transform.localScale = selected ? Vector3.one * 1.18f : Vector3.one;
            spriteRenderer.sortingOrder = selected ? 2 : 0;
            ApplyVisual(selected ? 0.25f : 0f);
        }

        public void SetLinkHint(bool hinted)
        {
            if (!IsLinkable || clearing)
            {
                return;
            }

            this.hinted = hinted;
            if (selected)
            {
                return;
            }

            transform.localScale = hinted ? Vector3.one * 1.08f : Vector3.one;
            spriteRenderer.sortingOrder = hinted ? 1 : 0;
            ApplyVisual(hinted ? 0.18f : 0f);
        }

        private void Update()
        {
            if (clearing)
            {
                return;
            }

            if (statusOverlay != null && statusOverlay.enabled)
            {
                var overlayPulse = IsFrozen
                    ? 0.78f + Mathf.Sin(Time.time * 5f) * 0.12f
                    : 0.84f + Mathf.Sin(Time.time * 3.5f) * 0.06f;
                var color = statusOverlay.color;
                color.a = overlayPulse;
                statusOverlay.color = color;
            }

            if (!hinted || selected || !IsLinkable)
            {
                return;
            }

            var pulse = 1.05f + Mathf.Sin(Time.time * 8f) * 0.035f;
            transform.localScale = Vector3.one * pulse;
        }

        private void ApplyVisual(float whiteBlend = 0f)
        {
            if (spriteRenderer == null)
            {
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
                ApplyStatusOverlay(new Color(0.72f, 0.9f, 1f, 0.82f), 1.07f, 4);
            }
            else if (IsStone)
            {
                color = Color.Lerp(color, StoneTint, 0.6f);
                ApplyStatusOverlay(new Color(0.38f, 0.38f, 0.38f, 0.88f), 1.04f, 3);
            }
            else if (IsClock)
            {
                color = Color.Lerp(color, ClockTint, 0.4f);
                HideStatusOverlay();
            }
            else
            {
                HideStatusOverlay();
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
            HideStatusOverlay();
        }

        private IEnumerator AnimateClearAndDestroy(float duration)
        {
            var startScale = transform.localScale;
            var startColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
            var overlayStartColor = statusOverlay != null ? statusOverlay.color : Color.clear;
            var time = 0f;
            while (time < duration)
            {
                var t = time / duration;
                var pop = t < 0.38f
                    ? Mathf.Lerp(1f, 1.24f, t / 0.38f)
                    : Mathf.Lerp(1.24f, 0.12f, (t - 0.38f) / 0.62f);
                transform.localScale = startScale * pop;
                if (spriteRenderer != null)
                {
                    var color = startColor;
                    color.a = Mathf.Lerp(startColor.a, 0f, Mathf.Clamp01((t - 0.22f) / 0.78f));
                    spriteRenderer.color = color;
                    spriteRenderer.sortingOrder = 12;
                }

                if (statusOverlay != null)
                {
                    var color = overlayStartColor;
                    color.a = Mathf.Lerp(overlayStartColor.a, 0f, t);
                    statusOverlay.color = color;
                    statusOverlay.sortingOrder = 13;
                }

                time += Time.deltaTime;
                yield return null;
            }

            Destroy(gameObject);
        }

        private void EnsureStatusOverlay()
        {
            if (statusOverlay != null)
            {
                return;
            }

            var overlayObject = new GameObject("StatusOverlay");
            overlayObject.transform.SetParent(transform, false);
            overlayObject.transform.localPosition = Vector3.back * 0.02f;
            overlayObject.transform.localScale = Vector3.one;
            statusOverlay = overlayObject.AddComponent<SpriteRenderer>();
            statusOverlay.sprite = GetOrCreateStatusOverlaySprite();
            statusOverlay.enabled = false;
            statusOverlay.sortingOrder = 3;
        }

        private void ApplyStatusOverlay(Color color, float scale, int sortingOrder)
        {
            EnsureStatusOverlay();
            statusOverlay.enabled = true;
            statusOverlay.color = color;
            statusOverlay.sortingOrder = sortingOrder;
            statusOverlay.transform.localScale = Vector3.one * scale;
        }

        private void HideStatusOverlay()
        {
            if (statusOverlay != null)
            {
                statusOverlay.enabled = false;
            }
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

        private static Sprite GetOrCreateStatusOverlaySprite()
        {
            if (statusOverlaySprite != null)
            {
                return statusOverlaySprite;
            }

            const int size = 96;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            var outerRadius = size * 0.45f;
            var innerRadius = size * 0.35f;
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
                    texture.SetPixel(x, y, inOuter && !inInner ? Color.white : new Color(0f, 0f, 0f, 0f));
                }
            }

            texture.Apply();
            statusOverlaySprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return statusOverlaySprite;
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
