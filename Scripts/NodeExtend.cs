using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Node
{
    public enum NodeType
    {
        Red = 0,
        Green,
        Blue,
    }
    public enum Neighbour
    {
        None = 0,
        N = 2,
        NE = 4,
        E = 8,
        SE = 16,
        S = 32,
        SW = 64,
        W = 128,
        NW = 256
    }

    public class Primary
    {
        public Color color;
        public NodeType type;
        public int neighbours;
    }

}
