using UnityEngine;

namespace PipeHack.Data
{
    /// <summary>
    /// Plain data for a single grid cell. Deliberately NOT a MonoBehaviour -
    /// this is the "source of truth" that GridManager owns and TileView
    /// merely displays. Flow Validation / Puzzle Generation should read
    /// this class, not the visuals.
    /// </summary>
    public class TileData
    {
        public Vector2Int GridPosition { get; private set; }
        public PipeDefinition Pipe { get; set; }
        public bool IsRevealed { get; set; }
        public bool IsBlocker => Pipe != null && Pipe.category == PipeCategory.Blocker;

        public TileData(Vector2Int gridPosition, PipeDefinition pipe)
        {
            GridPosition = gridPosition;
            Pipe = pipe;
            IsRevealed = false;
        }
    }
}