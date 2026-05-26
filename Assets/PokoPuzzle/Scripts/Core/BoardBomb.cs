using System.Collections.Generic;
using UnityEngine;

namespace PokoPuzzle.Core
{
    public enum BombType
    {
        Red,
        Blue
    }

    public static class BoardBomb
    {
        private static readonly Vector2Int[] RedBombDirections =
        {
            new Vector2Int(0, -1),
            new Vector2Int(0, 1),
            new Vector2Int(-1, 0),
            new Vector2Int(1, 0),
            new Vector2Int(-1, 1),
            new Vector2Int(1, -1)
        };

        public static IEnumerable<Vector2Int> GetAffectedPositions(int column, int row, int height, BombType bombType)
        {
            if (bombType == BombType.Red)
            {
                return GetRedBombPositions(column, row, height);
            }

            return GetBlueBombPositions(column, row, height);
        }

        private static IEnumerable<Vector2Int> GetRedBombPositions(int column, int row, int height)
        {
            var affected = new HashSet<Vector2Int>();
            affected.Add(new Vector2Int(column, row));

            foreach (var dir in RedBombDirections)
            {
                var c = column + dir.x;
                var r = row + dir.y;

                while (r >= 0 && r < height && c >= 0)
                {
                    if (c >= HexGridUtility.RowSize(r))
                    {
                        break;
                    }

                    affected.Add(new Vector2Int(c, r));
                    c += dir.x;
                    r += dir.y;
                }
            }

            return affected;
        }

        private static IEnumerable<Vector2Int> GetBlueBombPositions(int column, int row, int height)
        {
            var affected = new HashSet<Vector2Int>();

            for (var dr = -2; dr <= 2; dr++)
            {
                for (var dc = -2; dc <= 2; dc++)
                {
                    if (Mathf.Abs(dr) + Mathf.Abs(dc) > 3)
                    {
                        continue;
                    }

                    var c = column + dc;
                    var r = row + dr;

                    if (r < 0 || r >= height || c < 0)
                    {
                        continue;
                    }

                    if (c >= HexGridUtility.RowSize(r))
                    {
                        continue;
                    }

                    affected.Add(new Vector2Int(c, r));
                }
            }

            return affected;
        }
    }
}
