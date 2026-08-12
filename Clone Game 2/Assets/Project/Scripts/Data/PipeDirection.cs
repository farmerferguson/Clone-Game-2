using System;

namespace PipeHack.Data
{
    /// <summary>
    /// Bitmask of which sides of a tile a pipe connects to.
    /// A "Straight" pipe uses two opposite flags (North|South or East|West).
    /// An "Elbow" pipe uses two adjacent flags (e.g. North|East).
    /// Kept as a flags enum so the Flow Validation system can do
    /// cheap bitwise checks when walking the grid.
    /// </summary>
    [Flags]
    public enum PipeDirection
    {
        None = 0,
        North = 1 << 0,
        East = 1 << 1,
        South = 1 << 2,
        West = 1 << 3,
    }

    public static class PipeDirectionUtil
    {
        /// <summary>Returns the opposite side (North<->South, East<->West).</summary>
        public static PipeDirection Opposite(PipeDirection dir)
        {
            switch (dir)
            {
                case PipeDirection.North: return PipeDirection.South;
                case PipeDirection.South: return PipeDirection.North;
                case PipeDirection.East: return PipeDirection.West;
                case PipeDirection.West: return PipeDirection.East;
                default: return PipeDirection.None;
            }
        }

        /// <summary>Grid-space offset for a single direction flag (Y+ = North).</summary>
        public static UnityEngine.Vector2Int ToOffset(PipeDirection dir)
        {
            switch (dir)
            {
                case PipeDirection.North: return new UnityEngine.Vector2Int(0, 1);
                case PipeDirection.South: return new UnityEngine.Vector2Int(0, -1);
                case PipeDirection.East: return new UnityEngine.Vector2Int(1, 0);
                case PipeDirection.West: return new UnityEngine.Vector2Int(-1, 0);
                default: return UnityEngine.Vector2Int.zero;
            }
        }
    }
}
