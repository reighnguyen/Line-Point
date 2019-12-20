using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AStarPathFinding
{
    class Tracking
    {
        public IPoint point;
        public Tracking parent;

        public float h;
        public float g;
        public float f
        {
            get
            {
                return h + g;
            }
        }
        public int id
        {
            get
            {
                return point.GetUniqueId();
            }
        }
    }

    public interface IPoint
    {
        int GetUniqueId();
    }

    private Dictionary<int, Tracking> _opened;
    private Dictionary<int, Tracking> _closed;

    public bool Find(IPoint start, IPoint end, Func<IPoint, List<IPoint>> possibleMoveFunc, Func<IPoint, float> heuristicFunc)
    {
        if (_opened == null)
        {
            _opened = new Dictionary<int, Tracking>();
            _closed = new Dictionary<int, Tracking>();
        }

        _opened.Clear();
        _closed.Clear();

        Tracking startTracking = new Tracking()
        {
            parent = null,
            point = start,
            g = 0,
            h = heuristicFunc(start),
        };
        Tracking endTracking = new Tracking()
        {
            parent = null,
            point = end,
            g = 0,
            h = 0,
        };

        _opened.Add(start.GetUniqueId(), startTracking);
        return Find(startTracking, endTracking, possibleMoveFunc, heuristicFunc, ref _closed, ref _opened);
    }

    private bool Find(Tracking current, Tracking end, Func<IPoint, List<IPoint>> possibleMoveFunc, Func<IPoint, float> heuristicFunc, ref Dictionary<int, Tracking> closed, ref Dictionary<int, Tracking> opened)
    {
        if (opened.Count != 0 && current != null)
        {
            closed.Add(current.id, current);
            opened.Remove(current.id);
            if (current.id != end.id)
            {
                List<IPoint> nextPoints = possibleMoveFunc.Invoke(current.point);
                if (nextPoints != null)
                {
                    for (int i = 0; i < nextPoints.Count; i++)
                    {
                        IPoint p = nextPoints[i];
                        if (!closed.ContainsKey(p.GetUniqueId()) && !opened.ContainsKey(p.GetUniqueId()))
                        {
                            opened.Add(p.GetUniqueId(), new Tracking()
                            {
                                parent = current,
                                point = p,
                                h = heuristicFunc(p),
                                g = current.g + 1,
                            });
                        }
                    }
                }

                Tracking nextChecking = null;
                foreach (var it in opened)
                {
                    if (nextChecking == null)
                    {
                        nextChecking = it.Value;
                    }
                    else
                    {
                        if (nextChecking.f > it.Value.f)
                        {
                            nextChecking = it.Value;
                        }
                    }
                }
                return Find(nextChecking, end, possibleMoveFunc, heuristicFunc, ref closed, ref opened);
            }
            else
            {
                // gotten goal
                return true;
            }
        }
        return false;
    }

    public void GetTracking(IPoint curPoint, ref List<IPoint> path)
    {
        if (_closed.ContainsKey(curPoint.GetUniqueId()))
        {
            DoTracking(curPoint, _closed[curPoint.GetUniqueId()], _closed, ref path);
        }
    }

    private void DoTracking(IPoint curPoint, Tracking curTracking, Dictionary<int, Tracking> closed, ref List<IPoint> path)
    {
        if (closed.ContainsKey(curPoint.GetUniqueId()))
        {
            if (curTracking.parent != null)
            {
                path.Add(curTracking.parent.point);
                DoTracking(curTracking.parent.point, curTracking.parent, closed, ref path);
            }
        }
    }
}
