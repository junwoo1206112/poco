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
        private static Sprite petrifiedOverlaySprite;
        private static Sprite redBombSprite;
        private static Sprite blueBombSprite;

        public int Column { get; private set; }
        public int Row { get; private set; }
        public PokoTileType Type { get; private set; }
        public PokoBlockSubtype BlockSubtype { get; private set; }
        public bool IsBomb { get; private set; }
        public BombType BombType { get; private set; }
        public int BlockHitPoints { get; private set; }
        public bool IsFrozen => BlockSubtype == PokoBlockSubtype.Frozen;
        public bool IsStone => BlockSubtype == PokoBlockSubtype.Stone;
        public bool IsPetrified => BlockSubtype == PokoBlockSubtype.Petrified;
        public bool IsLinkable => !clearing && BlockSubtype == PokoBlockSubtype.None && !IsBomb;
        public bool IsClearing => clearing;

        private float bombTimer = -1f;
        private bool selected;
        private bool hinted;
        private bool clearing;
        private SpriteRenderer statusOverlay;
        private const float BombAutoDetonateTime = 5f;
        private static readonly Color FrozenTint = new Color(0.88f, 0.96f, 1f, 1f);
        private static readonly Color StoneTint = new Color(0.72f, 0.72f, 0.72f, 1f);
        private static readonly Color PetrifiedTint = new Color(0.72f, 0.42f, 1f, 1f);

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
            BlockHitPoints = 0;
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
            var hitPoints = subtype == PokoBlockSubtype.Stone ? 2 : 0;
            ConfigureSubtype(subtype, hitPoints);
        }

        public void ConfigureSubtype(PokoBlockSubtype subtype, int hitPoints)
        {
            BlockSubtype = subtype;
            BlockHitPoints = subtype == PokoBlockSubtype.Stone ? Mathf.Max(1, hitPoints) : 0;
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
            BlockHitPoints = 0;
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
                spriteRenderer.sprite = GetOrCreateBombSprite(bombType);
                spriteRenderer.color = Color.white;
                ApplyBombVisual();
            }
        }

        public bool DamageStone()
        {
            if (!IsStone)
            {
                return false;
            }

            BlockHitPoints = Mathf.Max(0, BlockHitPoints - 1);
            ApplyVisual();
            return BlockHitPoints == 0;
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
                spriteRenderer.sortingOrder = 4;
            }
            else
            {
                spriteRenderer.color = Color.white;
                spriteRenderer.sortingOrder = 3;
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

        public void PlayPullToward(Vector3 targetPosition, float duration, float delay)
        {
            if (clearing)
            {
                return;
            }

            selected = false;
            hinted = false;
            if (hexCollider != null)
            {
                hexCollider.enabled = false;
            }

            StartCoroutine(AnimatePullToward(targetPosition, duration, delay));
        }

        public void PlayBlastAwayFrom(Vector3 origin, float duration, float delay)
        {
            if (clearing)
            {
                return;
            }

            selected = false;
            hinted = false;
            if (hexCollider != null)
            {
                hexCollider.enabled = false;
            }

            StartCoroutine(AnimateBlastAwayFrom(origin, duration, delay));
        }

        public void AnimateDrop(Vector3 targetPosition, float height, float delay)
        {
            if (clearing)
            {
                return;
            }

            StartCoroutine(AnimateDropCoroutine(targetPosition, height, delay));
        }

        private IEnumerator AnimateDropCoroutine(Vector3 targetPosition, float height, float delay)
        {
            var startPosition = new Vector3(targetPosition.x, targetPosition.y + height, targetPosition.z);
            transform.position = startPosition;
            yield return new WaitForSeconds(delay);

            var duration = Mathf.Lerp(0.08f, 0.18f, Mathf.Clamp01(height * 0.25f));
            var time = 0f;
            while (time < duration)
            {
                var t = time / duration;
                var eased = 1f - Mathf.Pow(1f - t, 2.5f);
                transform.position = Vector3.Lerp(startPosition, targetPosition, eased);
                time += Time.deltaTime;
                yield return null;
            }

            transform.position = targetPosition;
        }

        public void PlayRainbowTargetPulse(Color targetColor, float duration, float delay)
        {
            if (clearing)
            {
                return;
            }

            selected = false;
            hinted = false;
            StartCoroutine(AnimateRainbowTargetPulse(targetColor, duration, delay));
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

            if (IsBomb)
            {
                return;
            }

            var color = Color.white;

            if (IsFrozen)
            {
                color = Color.Lerp(color, FrozenTint, 0.2f);
                ApplyStatusOverlay(new Color(0.68f, 0.92f, 1f, 0.72f), 1.08f, 4);
            }
            else if (IsStone)
            {
                var tintWeight = BlockHitPoints <= 1 ? 0.24f : 0.36f;
                color = Color.Lerp(color, StoneTint, tintWeight);
                ApplyStatusOverlay(new Color(0.24f, 0.24f, 0.24f, 0.76f), 1.05f, 3);
            }
            else if (IsPetrified)
            {
                color = Color.Lerp(color, PetrifiedTint, 0.42f);
                ApplyStatusOverlay(GetOrCreatePetrifiedOverlaySprite(), new Color(0.5f, 0.12f, 0.82f, 0.84f), 1.12f, 5);
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

            spriteRenderer.color = Color.white;
            spriteRenderer.sortingOrder = 3;

            var overlayColor = BombType == BombType.Red
                ? new Color(1f, 0.22f, 0.22f, 0.7f)
                : new Color(0.22f, 0.44f, 1f, 0.7f);
            ApplyStatusOverlay(overlayColor, 1.1f, 4);
        }

        private IEnumerator AnimateClearAndDestroy(float duration)
        {
            var startScale = transform.localScale;
            var startPosition = transform.localPosition;
            var shakeStrength = 0.045f;
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
                var shakeDamp = 1f - t;
                transform.localPosition = startPosition + new Vector3(
                    Mathf.Sin(t * 78f) * shakeStrength * shakeDamp,
                    Mathf.Cos(t * 63f) * shakeStrength * 0.65f * shakeDamp,
                    0f);
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

        private IEnumerator AnimatePullToward(Vector3 targetPosition, float duration, float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            var startPosition = transform.position;
            var startScale = transform.localScale;
            var startColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
            var pullTarget = new Vector3(targetPosition.x, targetPosition.y, startPosition.z);
            var side = Vector3.Cross(Vector3.forward, pullTarget - startPosition).normalized;
            if (side.sqrMagnitude < 0.001f)
            {
                side = Vector3.right;
            }

            var time = 0f;
            while (time < duration && !clearing)
            {
                var t = time / duration;
                var eased = 1f - Mathf.Pow(1f - t, 2.4f);
                var wobble = Mathf.Sin(t * Mathf.PI * 2.5f) * 0.12f * (1f - t);
                transform.position = Vector3.Lerp(startPosition, pullTarget, eased) + side * wobble;
                transform.localScale = startScale * Mathf.Lerp(1f, 0.32f, eased);

                if (spriteRenderer != null)
                {
                    var color = startColor;
                    color.a = Mathf.Lerp(startColor.a, 0.78f, t);
                    spriteRenderer.color = color;
                    spriteRenderer.sortingOrder = 11;
                }

                if (statusOverlay != null)
                {
                    statusOverlay.sortingOrder = 12;
                }

                time += Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator AnimateBlastAwayFrom(Vector3 origin, float duration, float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            var startPosition = transform.position;
            var startScale = transform.localScale;
            var startColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
            var direction = startPosition - origin;
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = Vector3.up;
            }

            direction.z = 0f;
            direction.Normalize();
            var endPosition = startPosition + direction * 0.42f;
            var spin = Random.Range(-360f, 360f);
            var time = 0f;

            while (time < duration && !clearing)
            {
                var t = time / duration;
                var eased = 1f - Mathf.Pow(1f - t, 2f);
                transform.position = Vector3.Lerp(startPosition, endPosition, eased);
                transform.localScale = startScale * Mathf.Lerp(1f, 1.22f, Mathf.Sin(t * Mathf.PI));
                transform.Rotate(0f, 0f, spin * Time.deltaTime);

                if (spriteRenderer != null)
                {
                    var color = Color.Lerp(startColor, new Color(1f, 0.62f, 0.12f, startColor.a), Mathf.Sin(t * Mathf.PI));
                    color.a = Mathf.Lerp(startColor.a, 0.86f, t);
                    spriteRenderer.color = color;
                    spriteRenderer.sortingOrder = 11;
                }

                if (statusOverlay != null)
                {
                    statusOverlay.sortingOrder = 12;
                }

                time += Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator AnimateRainbowTargetPulse(Color targetColor, float duration, float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            var startScale = transform.localScale;
            var startColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
            var startSortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder : 0;
            var time = 0f;

            while (time < duration && !clearing)
            {
                var t = time / duration;
                var rainbow = Color.HSVToRGB(Mathf.Repeat(t * 2.4f, 1f), 0.72f, 1f);
                rainbow.a = startColor.a;
                transform.localScale = startScale * (1f + Mathf.Sin(t * Mathf.PI * 4f) * 0.12f);

                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.Lerp(targetColor, rainbow, 0.45f + Mathf.Sin(t * Mathf.PI * 6f) * 0.25f);
                    spriteRenderer.sortingOrder = 10;
                }

                if (statusOverlay != null)
                {
                    statusOverlay.sortingOrder = 11;
                }

                time += Time.deltaTime;
                yield return null;
            }

            if (!clearing)
            {
                transform.localScale = startScale;
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = startColor;
                    spriteRenderer.sortingOrder = startSortingOrder;
                }
            }
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
            ApplyStatusOverlay(GetOrCreateStatusOverlaySprite(), color, scale, sortingOrder);
        }

        private void ApplyStatusOverlay(Sprite sprite, Color color, float scale, int sortingOrder)
        {
            EnsureStatusOverlay();
            statusOverlay.enabled = true;
            statusOverlay.sprite = sprite;
            statusOverlay.color = color;
            statusOverlay.sortingOrder = sortingOrder;
            statusOverlay.transform.localScale = Vector3.one * scale;
        }

        private void HideStatusOverlay()
        {
            if (statusOverlay != null)
            {
                statusOverlay.enabled = false;
                statusOverlay.sprite = GetOrCreateStatusOverlaySprite();
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

        private static Sprite GetOrCreatePetrifiedOverlaySprite()
        {
            if (petrifiedOverlaySprite != null)
            {
                return petrifiedOverlaySprite;
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

            var crackA = new Vector2(center.x - size * 0.18f, center.y + size * 0.18f);
            var crackB = new Vector2(center.x + size * 0.08f, center.y + size * 0.02f);
            var crackC = new Vector2(center.x + size * 0.22f, center.y - size * 0.2f);
            var crackD = new Vector2(center.x - size * 0.04f, center.y - size * 0.28f);

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var point = new Vector2(x, y);
                    var inOuter = IsInsideHex(point, outer);
                    var inInner = IsInsideHex(point, inner);
                    var ring = inOuter && !inInner;
                    var crack = DistanceToSegment(point, crackA, crackB) <= 2.2f ||
                        DistanceToSegment(point, crackB, crackC) <= 2.2f ||
                        DistanceToSegment(point, crackB, crackD) <= 1.8f;
                    texture.SetPixel(x, y, ring || crack ? Color.white : new Color(0f, 0f, 0f, 0f));
                }
            }

            texture.Apply();
            petrifiedOverlaySprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return petrifiedOverlaySprite;
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

        private static Sprite GetOrCreateBombSprite(BombType bombType)
        {
            if (bombType == BombType.Red)
            {
                if (redBombSprite == null)
                {
                    redBombSprite = CreateBombSprite(BombType.Red);
                }

                return redBombSprite;
            }

            if (blueBombSprite == null)
            {
                blueBombSprite = CreateBombSprite(BombType.Blue);
            }

            return blueBombSprite;
        }

        private static Sprite CreateBombSprite(BombType bombType)
        {
            const int size = 96;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            var outerRadius = size * 0.43f;
            var outer = new Vector2[6];

            for (var index = 0; index < 6; index++)
            {
                var angle = Mathf.Deg2Rad * (60f * index + 30f);
                var cos = Mathf.Cos(angle);
                var sin = Mathf.Sin(angle);
                outer[index] = new Vector2(center.x + outerRadius * cos, center.y + outerRadius * sin);
            }

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var point = new Vector2(x, y);
                    var inOuter = IsInsideHex(point, outer);

                    if (!inOuter)
                    {
                        texture.SetPixel(x, y, new Color(0f, 0f, 0f, 0f));
                        continue;
                    }

                    texture.SetPixel(x, y, bombType == BombType.Red
                        ? RedBombPixel(point, center)
                        : BlueBombPixel(point, center));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Color RedBombPixel(Vector2 point, Vector2 center)
        {
            var offset = point - center;
            var distance = offset.magnitude;
            var angle = Mathf.Atan2(offset.y, offset.x);
            var spoke = Mathf.Abs(Mathf.Sin(angle * 3f));
            var isCore = distance <= 15f;
            var isSpoke = distance <= 32f && spoke <= 0.2f;
            var isOuterSpark = distance >= 27f && distance <= 36f && spoke <= 0.32f;
            var baseColor = Color.Lerp(new Color(0.52f, 0.02f, 0.02f, 1f), new Color(1f, 0.18f, 0.08f, 1f), Mathf.Clamp01(1f - distance / 46f));

            if (isOuterSpark)
            {
                return new Color(1f, 0.76f, 0.14f, 1f);
            }

            if (isSpoke)
            {
                return new Color(1f, 0.92f, 0.18f, 1f);
            }

            if (isCore)
            {
                return distance <= 8f ? Color.white : new Color(1f, 0.62f, 0.12f, 1f);
            }

            return baseColor;
        }

        private static Color BlueBombPixel(Vector2 point, Vector2 center)
        {
            var offset = point - center;
            var distance = offset.magnitude;
            var angle = Mathf.Atan2(offset.y, offset.x);
            var ring = Mathf.Abs(Mathf.Sin(distance * 0.34f + angle * 2.2f));
            var isCore = distance <= 12f;
            var isSwirl = distance <= 34f && ring <= 0.2f;
            var isOuterRing = distance >= 29f && distance <= 35f;
            var baseColor = Color.Lerp(new Color(0.02f, 0.08f, 0.5f, 1f), new Color(0.2f, 0.62f, 1f, 1f), Mathf.Clamp01(1f - distance / 48f));

            if (isOuterRing)
            {
                return new Color(0.52f, 0.9f, 1f, 1f);
            }

            if (isSwirl)
            {
                return new Color(0.82f, 0.96f, 1f, 1f);
            }

            if (isCore)
            {
                return distance <= 6f ? Color.white : new Color(0.54f, 0.88f, 1f, 1f);
            }

            return baseColor;
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

        private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            var segment = b - a;
            var t = Vector2.Dot(point - a, segment) / Mathf.Max(0.0001f, Vector2.Dot(segment, segment));
            t = Mathf.Clamp01(t);
            return Vector2.Distance(point, a + segment * t);
        }
    }
}
