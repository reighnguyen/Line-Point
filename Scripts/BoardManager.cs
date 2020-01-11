#define DEV_DEBUG
#undef DEV_DEBUG

using System;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    private const int MINIMUM_TO_GET_POINT = 5;

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
    private List<RectTransform> _nodes;
    [SerializeField]
    private ScoreManager _scoreManager;

    private NodeFactory _nodeFactory;

    public List<RectTransform> Nodes
    {
        get
        {
            if (_nodes == null)
            {
                _nodes = new List<RectTransform>();
            }
            return _nodes;
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

    private NodeRandomSystem _randSystem;
    private int _lastSelectedIndex = -1;
    private List<KeyValuePair<int, Node.Primary>> _readyToSpawn;
    private List<KeyValuePair<int, Node.Primary>> _nextSpawn;
    private List<Node> _lastSpawned;

    public bool isOctalDirs;
    public const int COL = 9;
    public const int ROW = 9;
    public bool locker;

    private bool _isGameOver;

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
            Node node = Nodes[i].GetComponent<Node>();
            node.IsActive = false;
        }
        _selectedNodes = new Queue<Node>();
        _randSystem = new NodeRandomSystem(NodeFactory);
        _nextSpawn = null;
        _lastSpawned = new List<Node>();
        _readyToSpawn = _randSystem.GetRandomList(3, new List<int>());

        GenerateNext();
    }

    private void Update()
    {
#if DEV_DEBUG
        UpdateStatus();
#endif
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
                    firstSel.StartMove(path, seccondSel, () => PointMoveFinish(firstSel, seccondSel));
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
        _lastSelectedIndex = seccondSel.GetUniqueId();
        _randSystem.Swap(firstSel, seccondSel);

        Node.Primary temp = firstSel.PrimaryInfo;
        firstSel.IsActive = false;

        seccondSel.Setup(temp);
        seccondSel.StartScale(0.3f, 1.0f, 1, () => CheckBoard(seccondSel));
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
                AddMoveablePoints(ref points, r, c, (cur as Node).neighbours);
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
        Dictionary<Node.Neighbour, List<AStarPathFinding.IPoint>> continualPoints = GetContinualPoints(endPoint);
        int score = CombinateDirections(endPoint, continualPoints, GenerateNext);
        IncreaseScore(score);
    }

    private void IncreaseScore(int score)
    {
        _scoreManager.InscreaseScore(score);
    }

    private void RecheckBoardAfterSpwan()
    {
        int score = 0;
        Action<int> checkEndGame = (scoreGetted) =>
        {
            if (_isGameOver)
            {
                if (scoreGetted == 0)
                {
                    Debug.Log("Game Over");
                }
                else
                {
                    _isGameOver = false;

                }
            }
        };

        int maxSpawn = _lastSpawned.Count;
        int spawnCount = 0;
        if (maxSpawn > 0)
        {
            for (int i = 0; i < _lastSpawned.Count; i++)
            {
                Dictionary<Node.Neighbour, List<AStarPathFinding.IPoint>> lineByLastSpawn = GetContinualPoints(_lastSpawned[i]);
                score += CombinateDirections(_lastSpawned[i], lineByLastSpawn, () =>
                    {
                        spawnCount++;
                        if (spawnCount == maxSpawn)
                        {
                            checkEndGame(score);
                        }
                    });
            }
            IncreaseScore(score);
        }
        else
        {
            Debug.LogError("What went wrong here!");
        }
    }

    private Dictionary<Node.Neighbour, List<AStarPathFinding.IPoint>> GetContinualPoints(AStarPathFinding.IPoint endPoint)
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
        return continualPoints;
    }

    private int CombinateDirections
        (AStarPathFinding.IPoint center, Dictionary<Node.Neighbour, List<AStarPathFinding.IPoint>> lines, Action finishAct)
    {
        List<AStarPathFinding.IPoint> collectPoints = new List<AStarPathFinding.IPoint>();
        List<AStarPathFinding.IPoint> ew = GetLine(center, lines, Node.Neighbour.E, Node.Neighbour.W);
        List<AStarPathFinding.IPoint> sn = GetLine(center, lines, Node.Neighbour.S, Node.Neighbour.N);

        int compo = 0;
        if (CheckLine(ew, ref compo))
        {
            collectPoints.AddRange(ew);
        }
        if (CheckLine(sn, ref compo))
        {
            collectPoints.AddRange(sn);
        }

        if (isOctalDirs)
        {
            List<AStarPathFinding.IPoint> nesw = GetLine(center, lines, Node.Neighbour.NE, Node.Neighbour.SW);
            List<AStarPathFinding.IPoint> nwse = GetLine(center, lines, Node.Neighbour.NW, Node.Neighbour.SE);
            if (CheckLine(nesw, ref compo))
            {
                collectPoints.AddRange(nesw);
            }
            if (CheckLine(nwse, ref compo))
            {
                collectPoints.AddRange(nwse);
            }
        }
        RestorePoints(collectPoints, () =>
         {
             locker = false;
             if (finishAct != null)
             {
                 finishAct.Invoke();
             }
         });
        return compo;
    }

    private void RestorePoints(List<AStarPathFinding.IPoint> points, Action finishAct)
    {
        int maxPoint = points.Count;
        if (maxPoint > 0)
        {
            int count = 0;
            for (int i = 0; i < maxPoint; i++)
            {
                Node node = GetNodeByIndex(points[i].GetUniqueId());
                if (node != null)
                {
                    _randSystem.Restore(node.GetUniqueId());
                    node.StartScale(1.0f, 0.3f, 1.0f, () =>
                    {
                        node.IsActive = false;

                        count++;
                        if (count == maxPoint)
                        {
                            finishAct.Invoke();
                        }
                    });
                }
            }
        }
        else
        {
            finishAct.Invoke();
        }
    }

    private bool CheckLine(List<AStarPathFinding.IPoint> line, ref int score)
    {
        int count = line.Count;
        if (count >= MINIMUM_TO_GET_POINT)
        {
            // equal to 1 + bonus
            score += (count / MINIMUM_TO_GET_POINT) + (count % MINIMUM_TO_GET_POINT);
            return true;
        }
        return false;
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
            if (next.PrimaryInfo != null && origin.PrimaryInfo != null)
            {
                if (next.PrimaryInfo.color == origin.PrimaryInfo.color)
                {
                    return next;
                }
            }
        }
        return null;
    }

    private void GenerateNext()
    {
        _lastSpawned.Clear();
        if (_readyToSpawn.Count > 0)
        {
            int count = 0;
            int maxAnimation = _readyToSpawn.Count;
            for (int i = 0; i < _readyToSpawn.Count; i++)
            {
                KeyValuePair<int, Node.Primary> nodeInfo = _readyToSpawn[i];
                if (_lastSelectedIndex == nodeInfo.Key)
                {
                    List<int> ignore = _readyToSpawn.ConvertAll<int>((kvp) => kvp.Key);
                    ignore.AddRange(_nextSpawn.ConvertAll<int>((kvp) => kvp.Key));
                    ignore.Add(_lastSelectedIndex);

                    KeyValuePair<int, Node.Primary> rdInfo = _randSystem.GetRandomNode(ignore);
                    if (rdInfo.Key != -1)
                    {
                        nodeInfo = rdInfo;
                        _readyToSpawn[i] = nodeInfo;
                    }
                    else
                    {
                        maxAnimation--;
                        continue;
                    }
                }
                NodeDic[nodeInfo.Key].Setup(nodeInfo.Value);
                NodeDic[nodeInfo.Key].StartScale(0.5f, 1.0f, 1, () =>
                {
                    count++;
                    if (count == maxAnimation)
                    {
                        RecheckBoardAfterSpwan();
                        _lastSpawned.Clear();
                    }
                });
                _randSystem.MarkNodeUsed(nodeInfo.Key);
                _lastSpawned.Add(NodeDic[nodeInfo.Key]);
            }
        }

        _nextSpawn = _randSystem.GetRandomList(10, new List<int>());
        for (int i = 0; i < _nextSpawn.Count; i++)
        {
            KeyValuePair<int, Node.Primary> nodeInfo = _nextSpawn[i];
            NodeDic[nodeInfo.Key].SetColor(nodeInfo.Value.color);
            NodeDic[nodeInfo.Key].StartScale(0.1f, 0.4f, 1, null);
        }
        if (_nextSpawn.Count != 0)
        {
            _readyToSpawn = _nextSpawn;
        }
        else
        {
            // maybe game end here
            _isGameOver = true;
        }
    }

    public Node GetPointByDirection(Node origin, Node.Neighbour dir)
    {
        if ((origin.neighbours & (int)dir) > 0)
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

    public Node GetNodeByIndex(int index)
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
        //Debug.Log(string.Format("{0} ::=> {1}", index, "out " + dirs));
        return dirs;
    }
    public static int ConvertTo1DIndex(int row, int col)
    {
        int r = Mathf.Clamp(row, 0, ROW);
        int c = Mathf.Clamp(col, 0, COL);
        return r * ROW + c;
    }
    public static void ConvertTo2DIndex(int index1D, out int row, out int col)
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
                node.gameObject.name = index.ToString();
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

#if DEV_DEBUG
    private const float ALPHA = 0.5f;
    public Color usedNode = new Color(1, 0, 0, ALPHA);
    public Color nextUseNode = new Color(1, 0.92f, 0.16f, ALPHA);
    public Color freeNode = new Color(1, 1, 1, 1);

    private void UpdateStatus()
    {
        foreach (KeyValuePair<int, Node> nodeKvp in NodeDic)
        {
            if (nodeKvp.Value.IsActive)
            {
                nodeKvp.Value.BackgroundImg.color = usedNode;
            }
            else if (_nextSpawn.Contains(kvp => kvp.Key == nodeKvp.Key))
            {
                nodeKvp.Value.BackgroundImg.color = nextUseNode;
            }
            else
            {
                nodeKvp.Value.BackgroundImg.color = freeNode;
            }
        }
    }
#endif

}
