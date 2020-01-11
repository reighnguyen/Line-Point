using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameController : MonoBehaviour
{
    private BoardManager _boardManager;

    private void Start()
    {
        _boardManager = BoardManager.Instance;
        this.SetupUI();
    }
}
