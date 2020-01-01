using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    private static BoardManager _instance;
    public static BoardManager Instance
    {
        get
        {
            return _instance;
        }
    }

    [SerializeField]
    private RectTransform _nodeTemplate;
    [SerializeField]
    private List<RectTransform> nodes;

    private NodeFactory _nodeFactory;

    public List<RectTransform> Nodes
    {
        get
        {
            if (nodes == null)
            {
                nodes = new List<RectTransform>();
            }
            return nodes;
        }
    }

    public NodeFactory NodeFactory
    {
        get
        {
            if (_nodeFactory == null)
            {
                _nodeFactory = new NodeFactory();
            }
            return _nodeFactory;
        }
    }

    private Queue<Node> _selectedNodes;

    private AStarPathFinding _aStar;
    private AStarPathFinding AStar
    {
        get
        {
            if (_aStar == null)
            {
                _aStar = new AStarPathFinding();
            }
            return _aStar;
        }
    }

    private Dictionary<int, Node> _nodeDic;
    private Dictionary<int, Node> NodeDic
    {
        get
        {
            if (_nodeDic == null)
            {
                _nodeDic = new Dictionary<int, Node>();
            }
            return _nodeDic;
        }
    }

    private Dictionary<Node.Neighbour, int[]> _directionPatterns;
    private Dictionary<Node.Neighbour, int[]> DirectionPatterns
    {
        get
        {
            if (_directionPatterns == null)
            {
                _directionPatterns = new Dictionary<Node.Neighbour, int[]>()
                {
                    { Node.Neighbour.N,  new int[2]{ -1, +0 } },
                    { Node.Neighbour.NE, new int[2]{ -1, +1 } },
                    { Node.Neighbour.E,  new int[2]{ +0, +1 } },
                    { Node.Neighbour.SE, new int[2]{ +1, +1 } },
                    { Node.Neighbour.S,  new int[2]{ +1, +0 } },
                    { Node.Neighbour.SW, new int[2]{ +1, -1 } },
                    { Node.Neighbour.W,  new int[2]{ +0, -1 } },
                    { Node.Neighbour.NW, new int[2]{ -1, -1 } },
                };
            }
            return _directionPatterns;
        }
    }

    public bool isOctalDirs;
    public const int COL = 9;
    public const int ROW = 9;
    public bool locker;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }

        for (int i = 0; i < Nodes.Count; i++)
        {
            Node node = Nodes[i].GetComponent<Node>();
            NodeDic.Add(node.GetUniqueId(), node);
        }
    }

    private void Start()
    {
        for (int i = 0; i < Nodes.Count; i++)
        {
            Node.Primary info = NodeFactory.GetRandomInfo();
            Node node = Nodes[i].GetComponent<Node>();
            node.StartScale(0, 1, 0.5f, null);
            node.ChangeInfo(info);

            if (i > Nodes.Count - 20)
            {
                node.IsActive = false;
            }
        }
        _selectedNodes = new Queue<Node>();
    }

    public void NodeSelected(Node node)
    {
        if (!locker)
        {
            locker = true;
            int count = _selectedNodes.Count;
            switch (count)
            {
                case 0:
                    if (node.IsActive)
                    {
                        _selectedNodes.Enqueue(node);
                        node.StartScale(1.2f, 1f, 0.5f, null);
                    }
                    break;
                case 1:
                    if (!node.IsActive)
                    {
                        _selectedNodes.Enqueue(node);
                    }
                    else
                    {
                        _selectedNodes.Clear();
                    }
                    break;
            }
            if (_selectedNodes.Count == 2)
            {
                Node firstSel = _selectedNodes.Dequeue();
                Node seccondSel = _selectedNodes.Dequeue();

                bool result = AStar.Find(firstSel, seccondSel, GetMoveablePoints, (p) =>
                    {
                        return CalDistance(p, seccondSel);
                    });
                if (result)
                {
                    List<AStarPathFinding.IPoint> path = new List<AStarPathFinding.IPoint>();
                    AStar.GetTracking(seccondSel, ref path);
                    firstSel.StartMove(path, seccondSel, () =>
                      {
                          PointMoveFinish(firstSel, seccondSel);
                      });
                }
                else
                {
                    locker = false;
                }
            }
            else
            {
                locker = false;
            }
        }
    }

    private void PointMoveFinish(Node firstSel, Node seccondSel)
    {
        Node.Primary temp = firstSel.PrimaryInfo;
        firstSel.ChangeInfo(seccondSel.PrimaryInfo);
        seccondSel.ChangeInfo(temp);

        seccondSel.StartScale(0.3f, 1.0f, 1, () => CheckBoard(seccondSel));
        firstSel.IsActive = false;
    }

    public List<AStarPathFinding.IPoint> GetMoveablePoints(AStarPathFinding.IPoint cur)
    {
        int index = cur.GetUniqueId();
        if (index >= 0)
        {
            try
            {
                List<AStarPathFinding.IPoint> points = new List<AStarPathFinding.IPoint>();
                int r, c;
                ConvertTo2DIndex(index, out r, out c);
                AddMoveablePoints(ref points, r, c, (cur as Node).PrimaryInfo.neighbours);
                return points;
            }
            catch (System.Exception e)
            {
                Debug.Log(e);
            }
        }
        Debug.LogError("Not found " + index);
        return null;
    }

    private void CheckBoard(AStarPathFinding.IPoint endPoint)
    {
        Dictionary<Node.Neighbour, List<AStarPathFinding.IPoint>> continualPoints = new Dictionary<Node.Neighbour, List<AStarPathFinding.IPoint>>();
        continualPoints[Node.Neighbour.E] = AStar.GetContinuallyPoints(endPoint, (point) => GetNextPointByDirection(point as Node, Node.Neighbour.E));
        continualPoints[Node.Neighbour.N] = AStar.GetContinuallyPoints(endPoint, (point) => GetNextPointByDirection(point as Node, Node.Neighbour.N));
        continualPoints[Node.Neighbour.S] = AStar.GetContinuallyPoints(endPoint, (point) => GetNextPointByDirection(point as Node, Node.Neighbour.S));
        continualPoints[Node.Neighbour.W] = AStar.GetContinuallyPoints(endPoint, (point) => GetNextPointByDirection(point as Node, Node.Neighbour.W));
        if (isOctalDirs)
        {
            continualPoints[Node.Neighbour.NE] = AStar.GetContinuallyPoints(endPoint, (point) => GetNextPointByDirection(point as Node, Node.Neighbour.NE));
            continualPoints[Node.Neighbour.SE] = AStar.GetContinuallyPoints(endPoint, (point) => GetNextPointByDirection(point as Node, Node.Neighbour.SE));
            continualPoints[Node.Neighbour.SW] = AStar.GetContinuallyPoints(endPoint, (point) => GetNextPointByDirection(point as Node, Node.Neighbour.SW));
            continualPoints[Node.Neighbour.NW] = AStar.GetContinuallyPoints(endPoint, (point) => GetNextPointByDirection(point as Node, Node.Neighbour.NW));
        }
        CombinateDirections(endPoint, continualPoints);
    }

    private void CombinateDirections(AStarPathFinding.IPoint center, Dictionary<Node.Neighbour, List<AStarPathFinding.IPoint>> lines)
    {
        List<AStarPathFinding.IPoint> ew = GetLine(center, lines, Node.Neighbour.E, Node.Neighbour.W);
        List<AStarPathFinding.IPoint> sn = GetLine(center, lines, Node.Neighbour.S, Node.Neighbour.N);
        CheckLine(ew);
        CheckLine(sn);
        if (isOctalDirs)
        {
            List<AStarPathFinding.IPoint> nesw = GetLine(center, lines, Node.Neighbour.NE, Node.Neighbour.SW);
            List<AStarPathFinding.IPoint> nwse = GetLine(center, lines, Node.Neighbour.NW, Node.Neighbour.SE);
            CheckLine(nesw);
            CheckLine(nwse);
        }
        locker = false;
    }

    private void CheckLine(List<AStarPathFinding.IPoint> line)
    {
        if (line.Count >= 5)
        {
            for (int i = 0; i < line.Count; i++)
            {
                Node node = GetNodeByIndex(line[i].GetUniqueId());
                if (node != null)
                {
                    node.StartScale(1.0f, 0.3f, 1.0f, () => node.IsActive = false);
                }
            }
        }
    }

    private List<AStarPathFinding.IPoint> GetLine(AStarPathFinding.IPoint center, Dictionary<Node.Neighbour, List<AStarPathFinding.IPoint>> lines, Node.Neighbour dir1, Node.Neighbour dir2)
    {
        List<AStarPathFinding.IPoint> line = new List<AStarPathFinding.IPoint>();
        line.AddRange(lines[dir1]);
        line.Add(center);
        line.AddRange(lines[dir2]);
        return line;
    }

    private Node GetNextPointByDirection(Node origin, Node.Neighbour dir)
    {
        Node next = GetPointByDirection(origin, dir);
        if (next != null && next.IsActive)
        {
            if (next.PrimaryInfo.color == origin.PrimaryInfo.color)
            {
                return next;
            }
        }
        return null;
    }

    private Node GetPointByDirection(Node origin, Node.Neighbour dir)
    {
        if ((origin.PrimaryInfo.neighbours & (int)dir) > 0)
        {
            int[] pattern;
            DirectionPatterns.TryGetValue(dir, out pattern);
            if (pattern != null)
            {
                int r, c;
                ConvertTo2DIndex(origin.GetUniqueId(), out r, out c);
                int index = ConvertTo1DIndex(r + pattern[0], c + pattern[1]);
                return GetNodeByIndex(index);
            }
        }
        return null;
    }

    private void AddMoveablePoints(ref List<AStarPathFinding.IPoint> points, int curR, int curC, int directions)
    {
        if ((directions & (int)Node.Neighbour.N) > 0)
        {
            Node node = GetNodeByIndex(ConvertTo1DIndex(curR - 1, curC));
            if (!node.IsActive)
            {
                points.Add(node);
            }
        }
        if ((directions & (int)Node.Neighbour.NE) > 0)
        {
            Node node = GetNodeByIndex(ConvertTo1DIndex(curR - 1, curC + 1));
            if (!node.IsActive)
            {
                points.Add(node);
            }
        }
        if ((directions & (int)Node.Neighbour.E) > 0)
        {
            Node node = GetNodeByIndex(ConvertTo1DIndex(curR, curC + 1));
            if (!node.IsActive)
            {
                points.Add(node);
            }
        }
        if ((directions & (int)Node.Neighbour.SE) > 0)
        {
            Node node = GetNodeByIndex(ConvertTo1DIndex(curR + 1, curC + 1));
            if (!node.IsActive)
            {
                points.Add(node);
            }
        }
        if ((directions & (int)Node.Neighbour.S) > 0)
        {
            Node node = GetNodeByIndex(ConvertTo1DIndex(curR + 1, curC));
            if (!node.IsActive)
            {
                points.Add(node);
            }
        }
        if ((directions & (int)Node.Neighbour.SW) > 0)
        {
            Node node = GetNodeByIndex(ConvertTo1DIndex(curR + 1, curC - 1));
            if (!node.IsActive)
            {
                points.Add(node);
            }
        }
        if ((directions & (int)Node.Neighbour.W) > 0)
        {
            Node node = GetNodeByIndex(ConvertTo1DIndex(curR, curC - 1));
            if (!node.IsActive)
            {
                points.Add(node);
            }
        }
        if ((directions & (int)Node.Neighbour.NW) > 0)
        {
            Node node = GetNodeByIndex(ConvertTo1DIndex(curR - 1, curC - 1));
            if (!node.IsActive)
            {
                points.Add(node);
            }
        }
    }

    private Node GetNodeByIndex(int index)
    {
        if (NodeDic.ContainsKey(index))
        {
            return NodeDic[index];
        }
        else
        {
            Debug.Log("Node found " + index);
            return null;
        }
    }

    private float CalDistance(AStarPathFinding.IPoint from, AStarPathFinding.IPoint to)
    {
        return Vector3.Distance(GetNodeByIndex(from.GetUniqueId()).transform.localPosition, GetNodeByIndex(to.GetUniqueId()).transform.localPosition);
    }

    public static int GetDirectionsByIndex(int index)
    {
        int r = -1;
        int c = -1;
        int dirs = (int)Node.Neighbour.None;
        ConvertTo2DIndex(index, out r, out c);
        if (r >= 0 && c >= 0)
        {
            // S,N
            if (r == 0)
            {
                dirs = dirs | (int)Node.Neighbour.S;
            }
            else
            {
                if (r < ROW - 1)
                {
                    dirs |= (int)Node.Neighbour.S;
                    dirs |= (int)Node.Neighbour.N;
                }
                else
                {
                    dirs |= (int)Node.Neighbour.N;
                }
            }

            // E,W
            if (c == 0)
            {
                dirs = dirs | (int)Node.Neighbour.E;
            }
            else
            {
                if (c < COL - 1)
                {
                    dirs |= (int)Node.Neighbour.E;
                    dirs |= (int)Node.Neighbour.W;
                }
                else
                {
                    dirs |= (int)Node.Neighbour.W;
                }
            }
            if (BoardManager.Instance.isOctalDirs)
            {
                // SE
                if ((dirs & (int)Node.Neighbour.S) > 0 && (dirs & (int)Node.Neighbour.E) > 0)
                {
                    dirs |= (int)Node.Neighbour.SE;
                }
                // SW
                if ((dirs & (int)Node.Neighbour.S) > 0 && (dirs & (int)Node.Neighbour.W) > 0)
                {
                    dirs |= (int)Node.Neighbour.SW;
                }
                // NW
                if ((dirs & (int)Node.Neighbour.N) > 0 && (dirs & (int)Node.Neighbour.W) > 0)
                {
                    dirs |= (int)Node.Neighbour.NW;
                }
                // NE
                if ((dirs & (int)Node.Neighbour.N) > 0 && (dirs & (int)Node.Neighbour.E) > 0)
                {
                    dirs |= (int)Node.Neighbour.NE;
                }
            }
        }
        Debug.Log(string.Format("{0} ::=> {1}", index, "out " + dirs));
        return dirs;
    }
    private static int ConvertTo1DIndex(int row, int col)
    {
        int r = Mathf.Clamp(row, 0, ROW);
        int c = Mathf.Clamp(col, 0, COL);
        return r * ROW + c;
    }
    private static void ConvertTo2DIndex(int index1D, out int row, out int col)
    {
        if (index1D >= 0 && index1D <= ConvertTo1DIndex(ROW, COL))
        {
            row = index1D / COL;
            col = index1D % COL;
        }
        else
        {
            row = -1;
            col = -1;
        }
    }

#if UNITY_EDITOR
    public void GenerateBoard()
    {
        ClearBoard();
        for (int r = 0; r < ROW; r++)
        {
            for (int c = 0; c < COL; c++)
            {
                int index = ConvertTo1DIndex(r, c);
                RectTransform node = Instantiate(_nodeTemplate, transform);
                node.GetComponent<Node>().SetIndex(index);
                node.gameObject.SetActive(true);
                Nodes.Add(node);
            }
        }
    }

    public void ClearBoard()
    {
        for (int i = 0; i < Nodes.Count; i++)
        {
            DestroyImmediate(Nodes[i].gameObject);
        }
        Nodes.Clear();
    }
#endif
}
