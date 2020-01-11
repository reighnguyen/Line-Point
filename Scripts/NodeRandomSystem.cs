using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class NodeRandomSystem
{
    private BoardManager _boardManager;
    private NodeFactory _nodeFactory;
    private List<int> _freeNodeList;
    private List<int> _usedNodeList;

    public NodeRandomSystem(NodeFactory factory)
    {
        _boardManager = BoardManager.Instance;
        _nodeFactory = factory;
        _freeNodeList = new List<int>();
        _usedNodeList = new List<int>();

        Initalize();
    }

    public List<KeyValuePair<int, Node.Primary>> GetRandomList(int num, List<int> ignore)
    {
        List<KeyValuePair<int, Node.Primary>> rdList = new List<KeyValuePair<int, Node.Primary>>();
        for (int i = 0; i < num; i++)
        {
            var rd = GetRandomNode(ignore);
            if (rd.Key != -1)
            {
                rdList.Add(rd);
                ignore.Add(rd.Key);
            }
            else
            {
                // doesn't has any slot for point
                break;
            }
        }
        return rdList;
    }

    public KeyValuePair<int, Node.Primary> GetRandomNode(List<int> ignore)
    {
        List<int> tempFreeNode = new List<int>(_freeNodeList);
        tempFreeNode.RemoveRange(ignore.GetEnumerator());
        int min = 0;
        int max = tempFreeNode.Count;
        //tempFreeNode.Log("Free Node: ",' ');
        if (max > 0)
        {
            int indexRd = UnityEngine.Random.Range(min, max);
            return new KeyValuePair<int, Node.Primary>(tempFreeNode[indexRd], _nodeFactory.GetRandomInfo());
        }
        else
        {
            return new KeyValuePair<int, Node.Primary>(-1, null);
        }
        
    }

    public void MarkNodeUsed(int nodeId)
    {
        _freeNodeList.Remove(nodeId);
        _usedNodeList.Add(nodeId);
    }

    private void Initalize()
    {
        for (int c = 0; c < BoardManager.COL; c++)
        {
            for (int r = 0; r < BoardManager.ROW; r++)
            {
                _freeNodeList.Add(BoardManager.ConvertTo1DIndex(c, r));
            }
        }
    }

    public void Swap(Node firstSel, Node seccondSel)
    {
        Restore(firstSel.GetUniqueId());
        MarkNodeUsed(seccondSel.GetUniqueId());
    }

    public void Restore(int id)
    {
        _usedNodeList.Remove(id);
        _freeNodeList.Add(id);
    }

    public void Restart()
    {
        _usedNodeList.Clear();
        _freeNodeList.Clear();
        Initalize();
    }
}
