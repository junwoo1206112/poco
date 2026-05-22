using UnityEngine;

namespace PokoPuzzle.Core
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(PolygonCollider2D))]
    public sealed class PokoTile : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private PolygonCollider2D hexCollider;

        public int Column { get; private set; }
        public int Row { get; private set; }
        public PokoTileType Type { get; private set; }

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
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = type.ToColor();
            name = $"Tile_{column}_{row}_{type}";
            SetHexCollider();
        }

        private void SetHexCollider()
        {
            var colliderRadius = 0.38f;
            var vertices = new Vector2[6];
            for (var index = 0; index < 6; index++)
            {
                var angle = Mathf.Deg2Rad * (60f * index);
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
            spriteRenderer.color = type.ToColor();
            name = $"Tile_{Column}_{Row}_{Type}";
        }

        public void SetSelected(bool selected)
        {
            transform.localScale = selected ? Vector3.one * 1.18f : Vector3.one;
            spriteRenderer.sortingOrder = selected ? 2 : 0;
            spriteRenderer.color = selected ? Color.Lerp(Type.ToColor(), Color.white, 0.25f) : Type.ToColor();
        }

        public void SetLinkHint(bool hinted)
        {
            transform.localScale = hinted ? Vector3.one * 1.08f : Vector3.one;
            spriteRenderer.sortingOrder = hinted ? 1 : 0;
            spriteRenderer.color = hinted ? Color.Lerp(Type.ToColor(), Color.white, 0.18f) : Type.ToColor();
        }
    }
}
