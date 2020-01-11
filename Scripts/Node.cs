using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class Node : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, AStarPathFinding.IPoint
{
    private const string DISPLAY_OBJECT = "Bound/Display";
    private const string BACKGROUND_OBJECT = "Bound/Background";

    [SerializeField]
    private int _index = -1;

    private Primary _info;

    private Image _displayImg;
    public Image DisplayImg
    {
        get
        {
            if (_displayImg == null)
            {
                _displayImg = transform.Find(DISPLAY_OBJECT).GetComponent<Image>();
            }
            return _displayImg;
        }
    }

    private Image _backgroundImg;
    public Image BackgroundImg
    {
        get
        {
            if (_backgroundImg == null)
            {
                _backgroundImg = transform.Find(BACKGROUND_OBJECT).GetComponent<Image>();
            }
            return _backgroundImg;
        }
    }

    [SerializeField]
    private bool _isActive;
    public bool IsActive
    {
        get
        {
            return _isActive;
        }
        set
        {
            if (!value)
            {
                ResetNode();
            }
            _isActive = value;
        }
    }
    public Primary PrimaryInfo
    {
        get
        {
            return _info;
        }
    }

    // for scale
    private float _targetScale;
    private float _curScale;
    private float _scaleFactor;
    private Vector3 _originScale;
    private bool _isScaling;
    private Action _finshScale;

    // pointer handle
    private bool _isPoiterDown;

    // move handle
    private bool _isMoving;
    private WaitForSeconds _waitToNextMove;
    private AStarPathFinding.IPoint _curLocation;
    private List<AStarPathFinding.IPoint> _path;

    public int neighbours;

    private void Start()
    {
        neighbours = BoardManager.GetDirectionsByIndex(_index);
    }

    public void SetIndex(int index)
    {
        if (!IsActive)
        {
            if (_index < 0)
            {
                _index = index;
            }
        }
    }

    public void Setup(Primary info, int index)
    {
        if (info != null)
        {
            if (_info == null)
            {
                _info = info;
                _index = index;
                SetColor(_info.color);
                IsActive = true;
            }
        }
    }

    public void Setup(Primary info)
    {
        _info = info;
        SetColor(_info.color);
        IsActive = true;
    }

    public void ResetNode()
    {
        _info = null;
        _isActive = false;
        SetColor(Color.clear);

        // scale
        _isScaling = false;
        DisplayImg.rectTransform.localScale = _originScale;
    }

    public void SetColor(Color color)
    {
        DisplayImg.color = color;
    }

    public void StartScale(float from, float to, float scaleFactor, Action finishAct)
    {
        if (!_isScaling && scaleFactor != 0)
        {
            _isScaling = true;
            _targetScale = to;
            _curScale = from;
            int factorSign = from <= to ? 1 : -1;
            _scaleFactor = Mathf.Abs(scaleFactor) * factorSign;
            _finshScale = finishAct;
        }
    }

    public void StartMove(List<AStarPathFinding.IPoint> path, AStarPathFinding.IPoint end, Action finishAct)
    {
        if (IsActive)
        {
            if (_path == null && !_isMoving)
            {
                _path = path;
                _isMoving = true;
                _path.Reverse();

                float scaleFactor = 1;
                float delayTime = 1.0f / scaleFactor;

                for (int i = 1; i < _path.Count; i++)
                {
                    (_path[i] as Node).StartScale(0.3f, 0.3f, scaleFactor, null);
                    (_path[i] as Node).SetColor(PrimaryInfo.color);
                }
                (end as Node).StartScale(0.3f, 0.3f, scaleFactor, null);
                (end as Node).SetColor(PrimaryInfo.color);

                StartCoroutine(MoveCoroutine(finishAct, end));
            }
        }
    }

    private void MoveFinish(Action finishAct)
    {
        for (int i = 0; i < _path.Count; i++)
        {
            (_path[i] as Node)._isActive = false;
            (_path[i] as Node).SetColor(Color.clear);
        }

        if (finishAct != null)
        {
            finishAct.Invoke();
        }

        _path = null;
        _isMoving = false;
    }

    private IEnumerator MoveCoroutine(Action finishAct, AStarPathFinding.IPoint end)
    {
        int curMoveIndex = 0;
        int pointCount = _path.Count;
        while (_isMoving)
        {
            if (curMoveIndex < pointCount)
            {
                _curLocation = _path[curMoveIndex];
                if (curMoveIndex == pointCount)
                {
                    _curLocation = end;
                }
                (_curLocation as Node).StartScale(0.3f, 0.4f, 1, null);

                curMoveIndex++;
                yield return _waitToNextMove;
            }
            else
            {
                _isMoving = false;
            }
        }
        (end as Node).StartScale(0.3f, 0.3f, 1, () => MoveFinish(finishAct));
        yield break;
    }

    private void Awake()
    {
        _originScale = DisplayImg.rectTransform.localScale;
        _waitToNextMove = new WaitForSeconds(0.1f);
    }

    private void Update()
    {
        if (_isScaling)
        {
            ScaleUpdate();
        }
    }

    private void ScaleUpdate()
    {
        if (_scaleFactor > 0)
        {
            ScaleUp();
        }
        else
        {
            ScaleDown();
        }
    }

    private void ScaleUp()
    {
        if (_curScale >= _targetScale)
        {
            _curScale = _targetScale;
            _isScaling = false;
            FireFinishScaleEvent();
        }
        else
        {
            _curScale += (_scaleFactor * Time.deltaTime);
        }
        DisplayImg.rectTransform.localScale = _originScale * _curScale;
    }

    private void ScaleDown()
    {
        if (_curScale <= _targetScale)
        {
            _curScale = _targetScale;
            _isScaling = false;
            FireFinishScaleEvent();
        }
        else
        {
            _curScale += (_scaleFactor * Time.deltaTime);
        }
        DisplayImg.rectTransform.localScale = _originScale * _curScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _isPoiterDown = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_isPoiterDown)
        {
            BoardManager.Instance.NodeSelected(this);
            _isPoiterDown = false;
        }
    }

    public int GetUniqueId()
    {
        return _index;
    }

    private void FireFinishScaleEvent()
    {
        Action tempFinish = _finshScale;
        _finshScale = null;
        if (tempFinish != null)
        {
            tempFinish.Invoke();
        }
    }


}
