using UnityEngine;

namespace PipeHack.Data
{
    /// <summary>
    /// Which outer edge of the grid something is attached to.
    /// Used by Start/End nodes, which sit OUTSIDE the grid rather
    /// than occupying a swappable tile.
    /// </summary>
    public enum GridSide
    {
        North,
        East,
        South,
        West
    }

    public static class GridSideUtil
    {
        /// <summary>World/grid-space offset pointing away from the grid on this side.</summary>
        public static Vector2Int ToOffset(GridSide side)
        {
            switch (side)
            {
                case GridSide.North: return new Vector2Int(0, 1);
                case GridSide.South: return new Vector2Int(0, -1);
                case GridSide.East: return new Vector2Int(1, 0);
                case GridSide.West: return new Vector2Int(-1, 0);
                default: return Vector2Int.zero;
            }
        }

        /// <summary>
        /// The PipeDirection flag that a border tile needs open on this side
        /// for the external node to connect into it. E.g. a node sitting on
        /// the West side needs the border tile's West opening.
        /// </summary>
        public static PipeDirection ToRequiredOpening(GridSide side)
        {
            switch (side)
            {
                case GridSide.North: return PipeDirection.North;
                case GridSide.South: return PipeDirection.South;
                case GridSide.East: return PipeDirection.East;
                case GridSide.West: return PipeDirection.West;
                default: return PipeDirection.None;
            }
        }

        /// <summary>Z-rotation (degrees) to visually orient a node sprite so it "points" into the grid.</summary>
        public static float ToInwardRotation(GridSide side)
        {
            switch (side)
            {
                case GridSide.North: return 180f;
                case GridSide.South: return 0f;
                case GridSide.East: return -90f; // i.e. 270f
                case GridSide.West: return 90f;
                default: return 0f;
            }
        }
    }
}
