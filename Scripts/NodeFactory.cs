using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeFactory
{
    public static Dictionary<Node.NodeType, Color> ColorSet = new Dictionary<Node.NodeType, Color>()
    {
        { Node.NodeType.Red, Color.red },
        { Node.NodeType.Green, Color.green },
        { Node.NodeType.Blue, Color.blue },
    };

    public Node.Primary GetRandomInfo()
    {
        int rd = Random.Range(0, ColorSet.Count);

        return new Node.Primary()
        {
            type = (Node.NodeType)rd,
            color = ColorSet[(Node.NodeType)rd],
            neighbours = (int)Node.Neighbour.None
        };
    }
}
