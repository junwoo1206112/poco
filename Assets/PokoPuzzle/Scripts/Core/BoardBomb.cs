using System.Collections.Generic;
using UnityEngine;

namespace PokoPuzzle.Core
{
    public enum BombType
    {
        Red,
        Blue,
        Rainbow
    }

    public static class BoardBomb
    {
        private const int BlueBombRadius = 2;

        public static IEnumerable<Vector2Int> GetAffectedPositions(int column, int row, int height, BombType bombType)
        {
            return GetAffectedPositions(column, row, 4, height, bombType);
        }

        public static IEnumerable<Vector2Int> GetAffectedPositions(int column, int row, int width, int height, BombType bombType)
        {
            if (bombType == BombType.Red)
            {
                return GetRedBombPositions(column, row, width, height);
            }

            if (bombType == BombType.Blue)
            {
                return GetBlueBombPositions(column, row, width, height);
            }

            return System.Array.Empty<Vector2Int>();
        }

        private static IEnumerable<Vector2Int> GetRedBombPositions(int column, int row, int width, int height)
        {
            var affected = new List<Vector2Int>();
            affected.Add(new Vector2Int(column, row));

            for (var direction = 0; direction < HexGridUtility.GetNeighborCount(); direction++)
            {
                var c = column;
                var r = row;
                while (HexGridUtility.TryGetDirectionalNeighbor(c, r, direction, width, height, out var next))
                {
                    affected.Add(next);
                    c = next.x;
                    r = next.y;
                }
            }

            return affected;
        }

        private static IEnumerable<Vector2Int> GetBlueBombPositions(int column, int row, int width, int height)
        {
            var visited = new HashSet<Vector2Int>();
            var affected = new List<Vector2Int>();
            var frontier = new Queue<(Vector2Int Position, int Distance)>();
            var origin = new Vector2Int(column, row);
            visited.Add(origin);
            affected.Add(origin);
            frontier.Enqueue((origin, 0));

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();
                if (current.Distance >= BlueBombRadius)
                {
                    continue;
                }

                foreach (var neighbor in HexGridUtility.GetNeighbors(current.Position.x, current.Position.y, width, height))
                {
                    if (visited.Add(neighbor))
                    {
                        affected.Add(neighbor);
                        frontier.Enqueue((neighbor, current.Distance + 1));
                    }
                }
            }

            return affected;
        }
    }
}
