using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class Node : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, AStarPathFinding.IPoint
{
    private const string DISPLAY_OBJECT = "Bound/Display";

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

    private bool _isActive;
    public bool IsActive
    {
        get
        {
            return _isActive;
        }
        set
        {
            if(!value)
            {
                SetColor(Color.clear);
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

    // pointer handle
    private bool _isPoiterDown;

    // move handle


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
                _info.neighbours = BoardManager.GetDirectionsByIndex(_index);
                SetColor(_info.color);
                IsActive = true;                
            }
        }
    }

    public void ChangeInfo(Primary info)
    {
        _info = null;
        Setup(info, _index);
    }

    public void ResetNode()
    {
        _info = null;
        IsActive = false;
        DisplayImg.color = Color.clear;

        // scale
        _isScaling = false;
        DisplayImg.rectTransform.localScale = _originScale;
    }

    public void SetColor(Color color)
    {
        DisplayImg.color = color;
    }

    public void StartScale(float from, float to, float scaleFactor)
    {
        if (!_isScaling && scaleFactor != 0)
        {
            _isScaling = true;
            _targetScale = to;
            _curScale = from;
            int factorSign = from < to ? 1 : -1;
            _scaleFactor = Mathf.Abs(scaleFactor) * factorSign;
        }
    }

    public void StartMove(List<AStarPathFinding.IPoint> path)
    {

    }

    private void Awake()
    {
        _originScale = DisplayImg.rectTransform.localScale;
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
        if(_scaleFactor > 0)
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
}
