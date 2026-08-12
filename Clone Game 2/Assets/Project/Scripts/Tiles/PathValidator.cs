using System.Collections.Generic;
using UnityEngine;
using PipeHack.Data;
using PipeHack.Grid;

namespace PipeHack.Validation
{
    public static class PathValidator
    {
        public static bool ValidatePath(GridManager gridManager)
        {
            TileData[,] grid = gridManager.GetGridData();
            EdgeNodeData start = gridManager.GetStartNode();
            EdgeNodeData end = gridManager.GetEndNode();

            PipeDirection startNeedsOpen = GridSideUtil.ToRequiredOpening(start.Side);
            TileData startTile = grid[start.AttachCell.x, start.AttachCell.y];

            if (!startTile.Pipe.openSides.HasFlag(startNeedsOpen))
                return false;

            var toVisit = new Queue<(Vector2Int pos, PipeDirection exitDir)>();
            var visited = new HashSet<Vector2Int>();
            visited.Add(start.AttachCell);

            foreach (PipeDirection dir in GetSetFlags(startTile.Pipe.openSides))
            {
                if (dir == startNeedsOpen) continue;
                toVisit.Enqueue((start.AttachCell, dir));
            }

            PipeDirection endNeedsOpen = GridSideUtil.ToRequiredOpening(end.Side);

            while (toVisit.Count > 0)
            {
                var (currentPos, exitDir) = toVisit.Dequeue();
                Vector2Int neighborPos = currentPos + PipeDirectionUtil.ToOffset(exitDir);

                if (currentPos == end.AttachCell && exitDir == endNeedsOpen)
                    return true;

                if (!InBounds(grid, neighborPos)) continue;
                if (visited.Contains(neighborPos)) continue;

                TileData neighborTile = grid[neighborPos.x, neighborPos.y];
                PipeDirection neededBack = PipeDirectionUtil.Opposite(exitDir);

                if (!neighborTile.Pipe.openSides.HasFlag(neededBack)) continue;

                visited.Add(neighborPos);

                foreach (PipeDirection dir in GetSetFlags(neighborTile.Pipe.openSides))
                {
                    if (dir == neededBack) continue;
                    toVisit.Enqueue((neighborPos, dir));
                }
            }

            return false;
        }

        private static IEnumerable<PipeDirection> GetSetFlags(PipeDirection openSides)
        {
            if (openSides.HasFlag(PipeDirection.North)) yield return PipeDirection.North;
            if (openSides.HasFlag(PipeDirection.South)) yield return PipeDirection.South;
            if (openSides.HasFlag(PipeDirection.East)) yield return PipeDirection.East;
            if (openSides.HasFlag(PipeDirection.West)) yield return PipeDirection.West;
        }

        private static bool InBounds(TileData[,] grid, Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < grid.GetLength(0) &&
                   pos.y >= 0 && pos.y < grid.GetLength(1);
        }
    }
}