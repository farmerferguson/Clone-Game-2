using System.Collections.Generic;
using UnityEngine;
using PipeHack.Data;
using PipeHack.Grid;

namespace PipeHack.Validation
{
    public class PathResult
    {
        public bool IsConnected;
        public List<Vector2Int> PathCells = new List<Vector2Int>(); // ordered: nearest-Start -> nearest-End
    }

    public static class PathValidator
    {
        public static PathResult ValidatePath(GridManager gridManager)
        {
            var result = new PathResult();

            TileData[,] grid = gridManager.GetGridData();
            EdgeNodeData start = gridManager.GetStartNode();
            EdgeNodeData end = gridManager.GetEndNode();

            PipeDirection startNeedsOpen = GridSideUtil.ToRequiredOpening(start.Side);
            TileData startTile = grid[start.AttachCell.x, start.AttachCell.y];

            if (!startTile.Pipe.openSides.HasFlag(startNeedsOpen))
                return result; // not connected

            var toVisit = new Queue<(Vector2Int pos, PipeDirection exitDir)>();
            var visited = new HashSet<Vector2Int>();
            var parent = new Dictionary<Vector2Int, Vector2Int>();
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
                {
                    result.IsConnected = true;
                    result.PathCells = ReconstructPath(parent, start.AttachCell, currentPos);
                    return result;
                }

                if (!InBounds(grid, neighborPos)) continue;
                if (visited.Contains(neighborPos)) continue;

                TileData neighborTile = grid[neighborPos.x, neighborPos.y];
                PipeDirection neededBack = PipeDirectionUtil.Opposite(exitDir);

                if (!neighborTile.Pipe.openSides.HasFlag(neededBack)) continue;

                visited.Add(neighborPos);
                parent[neighborPos] = currentPos;

                foreach (PipeDirection dir in GetSetFlags(neighborTile.Pipe.openSides))
                {
                    if (dir == neededBack) continue;
                    toVisit.Enqueue((neighborPos, dir));
                }
            }

            return result; // not connected
        }

        public static List<Vector2Int> GetConnectedFromStart(GridManager gridManager)
        {
            var connected = new List<Vector2Int>();

            TileData[,] grid = gridManager.GetGridData();
            EdgeNodeData start = gridManager.GetStartNode();

            PipeDirection startNeedsOpen = GridSideUtil.ToRequiredOpening(start.Side);
            TileData startTile = grid[start.AttachCell.x, start.AttachCell.y];

            if (!startTile.Pipe.openSides.HasFlag(startNeedsOpen))
                return connected; // Start itself isn't even connected yet

            connected.Add(start.AttachCell);

            var toVisit = new Queue<(Vector2Int pos, PipeDirection exitDir)>();
            var visited = new HashSet<Vector2Int> { start.AttachCell };

            foreach (PipeDirection dir in GetSetFlags(startTile.Pipe.openSides))
            {
                if (dir == startNeedsOpen) continue;
                toVisit.Enqueue((start.AttachCell, dir));
            }

            while (toVisit.Count > 0)
            {
                var (currentPos, exitDir) = toVisit.Dequeue();
                Vector2Int neighborPos = currentPos + PipeDirectionUtil.ToOffset(exitDir);

                if (!InBounds(grid, neighborPos)) continue;
                if (visited.Contains(neighborPos)) continue;

                TileData neighborTile = grid[neighborPos.x, neighborPos.y];
                PipeDirection neededBack = PipeDirectionUtil.Opposite(exitDir);

                if (!neighborTile.Pipe.openSides.HasFlag(neededBack)) continue;

                visited.Add(neighborPos);
                connected.Add(neighborPos);

                foreach (PipeDirection dir in GetSetFlags(neighborTile.Pipe.openSides))
                {
                    if (dir == neededBack) continue;
                    toVisit.Enqueue((neighborPos, dir));
                }
            }

            return connected;
        }

        private static List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> parent, Vector2Int startCell, Vector2Int lastCell)
        {
            var path = new List<Vector2Int> { lastCell };
            Vector2Int cursor = lastCell;

            while (cursor != startCell && parent.ContainsKey(cursor))
            {
                cursor = parent[cursor];
                path.Add(cursor);
            }

            path.Reverse(); // now ordered nearest-Start -> nearest-End
            return path;
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