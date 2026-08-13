using System.IO.Pipes;
using UnityEngine;

namespace PipeHack.Data
{
    public enum PipeCategory
    {
        Straight,
        Elbow, 
        Blocker
    }

    /// <summary>
    /// One immutable pipe "piece" variant. Since players can only swap
    /// pieces (never rotate them), every orientation needs its own asset
    /// e.g. Straight_NS, Straight_EW, Elbow_NE, Elbow_ES, Elbow_SW, Elbow_WN.
    /// </summary>
    [CreateAssetMenu(fileName = "PipeDefinition", menuName = "PipeHack/Pipe Definition")]
    public class PipeDefinition : ScriptableObject
    {
        public string pieceId;              
        public PipeCategory category;
        public PipeDirection openSides;


        [Header("State sprites")]
        public Sprite revealedSprite;
        public Sprite connectedSprite; // shown the instant this tile becomes part of a completed Start->End path
        public Sprite filledSprite;    // shown once the fill animation reaches this tile
    }
}