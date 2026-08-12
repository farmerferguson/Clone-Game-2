using UnityEngine;

namespace PipeHack.Data
{
    /// <summary>
    /// A Start or End connector attached to the OUTSIDE of a border cell -
    /// it is not itself a grid tile and can never be swapped. Flow
    /// Validation should treat this as the fixed entry/exit point of the
    /// puzzle: the path must connect from here, through AttachCell's
    /// required opening, to the other node's AttachCell.
    /// </summary>
    public class EdgeNodeData
    {
        public Vector2Int AttachCell { get; }
        public GridSide Side { get; }
        public bool IsStart { get; }
        public bool IsEnd => !IsStart;

        public EdgeNodeData(Vector2Int attachCell, GridSide side, bool isStart)
        {
            AttachCell = attachCell;
            Side = side;
            IsStart = isStart;
        }
    }
}