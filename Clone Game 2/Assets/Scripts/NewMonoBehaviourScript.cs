using System.Collections.Generic;
using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    public enum Direction { Up, Down, Left, Right }

    public enum TileType //conceptual directions not vectors
    {
        Empty,
        Start,
        End,
        StraightHorizontal,
        StraightVertical,
        ElbowUpRight,
        ElbowRightDown,
        ElbowDownLeft,
        ElbowLeftUp,
        Blocked
    }

    public static class TileConnections
    {
        public static readonly Dictionary<TileType, HashSet<Direction>> Openings = new()
    {
        { TileType.StraightHorizontal, new HashSet<Direction> { Direction.Left, Direction.Right } },
        { TileType.StraightVertical,   new HashSet<Direction> { Direction.Up, Direction.Down } },
        { TileType.ElbowUpRight,       new HashSet<Direction> { Direction.Up, Direction.Right } },
        { TileType.ElbowRightDown,     new HashSet<Direction> { Direction.Right, Direction.Down } },
        { TileType.ElbowDownLeft,      new HashSet<Direction> { Direction.Down, Direction.Left } },
        { TileType.ElbowLeftUp,        new HashSet<Direction> { Direction.Left, Direction.Up } },
        { TileType.Empty,              new HashSet<Direction>() },
        { TileType.Blocked,            new HashSet<Direction>() }
    };
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
