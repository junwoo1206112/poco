using System.Collections.Generic;
using UnityEngine;

namespace PokoPuzzle.Core
{
    public static class HexGridUtility
    {
        public const float VerticalSpacingRatio = 0.8660254f;

        private static readonly Vector2Int[] ThreeTileOffsets =
        {
            new Vector2Int(0, -1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, 0),
            new Vector2Int(1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(1, 1)
        };

        private static readonly Vector2Int[] FourTileOffsets =
        {
            new Vector2Int(-1, -1),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0),
            new Vector2Int(1, 0),
            new Vector2Int(-1, 1),
            new Vector2Int(0, 1)
        };

        public static int RowSize(int row)
        {
            return (row & 1) == 0 ? 3 : 4;
        }

        public static int RowSize(int row, int width)
        {
            var baseSize = Mathf.Max(3, width);
            return (row & 1) == 0 ? baseSize - 1 : baseSize;
        }

        public static Vector3 ToWorld(int column, int row, int width, int height, float spacing, bool useHexGrid)
        {
            if (!useHexGrid)
            {
                var squareX = (column - (width - 1) * 0.5f) * spacing;
                var squareY = (row - (height - 1) * 0.5f) * spacing;
                return new Vector3(squareX, squareY, 0f);
            }

            var maxRowSize = Mathf.Max(RowSize(0, width), RowSize(1, width));
            var oddShift = IsOddRow(row) ? -0.5f : 0f;
            var x = (column - (maxRowSize - 1) * 0.5f + oddShift) * spacing;
            var y = (row - (height - 1) * 0.5f) * spacing * VerticalSpacingRatio;
            return new Vector3(x, y, 0f);
        }

        public static bool AreAdjacent(int aColumn, int aRow, int bColumn, int bRow)
        {
            if (aColumn == bColumn && aRow == bRow)
            {
                return false;
            }

            foreach (var offset in GetNeighborOffsets(aRow))
            {
                if (aColumn + offset.x == bColumn && aRow + offset.y == bRow)
                {
                    return true;
                }
            }

            return false;
        }

        public static IEnumerable<Vector2Int> GetNeighbors(int column, int row, int height)
        {
            return GetNeighbors(column, row, 4, height);
        }

        public static IEnumerable<Vector2Int> GetNeighbors(int column, int row, int width, int height)
        {
            foreach (var offset in GetNeighborOffsets(row))
            {
                var nextCol = column + offset.x;
                var nextRow = row + offset.y;

                if (nextRow < 0 || nextRow >= height || nextCol < 0)
                {
                    continue;
                }

                if (nextCol < RowSize(nextRow, width))
                {
                    yield return new Vector2Int(nextCol, nextRow);
                }
            }
        }

        public static bool TryGetDirectionalNeighbor(int column, int row, int direction, int height, out Vector2Int neighbor)
        {
            return TryGetDirectionalNeighbor(column, row, direction, 4, height, out neighbor);
        }

        public static bool TryGetDirectionalNeighbor(int column, int row, int direction, int width, int height, out Vector2Int neighbor)
        {
            var offsets = GetNeighborOffsets(row);
            if (direction < 0 || direction >= offsets.Length)
            {
                neighbor = default;
                return false;
            }

            var offset = offsets[direction];
            var nextCol = column + offset.x;
            var nextRow = row + offset.y;
            if (nextRow < 0 || nextRow >= height || nextCol < 0 || nextCol >= RowSize(nextRow, width))
            {
                neighbor = default;
                return false;
            }

            neighbor = new Vector2Int(nextCol, nextRow);
            return true;
        }

        public static int GetNeighborCount()
        {
            return 6;
        }

        private static Vector2Int[] GetNeighborOffsets(int row)
        {
            return (row & 1) == 0 ? ThreeTileOffsets : FourTileOffsets;
        }

        private static bool IsOddRow(int row)
        {
            return (row & 1) == 1;
        }
    }
}
