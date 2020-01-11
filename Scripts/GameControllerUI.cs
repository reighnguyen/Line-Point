using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class GameController
{
    [SerializeField]
    private Button _startGameBtn;
    [SerializeField]
    private Button _restartGameBtn;

    private void SetupUI()
    {
        _startGameBtn.onClick.AddListener(OnStartGameButton);
        _restartGameBtn.onClick.AddListener(OnRestartGameButton);

        // start game the restart button will be disable
        _restartGameBtn.interactable = false;

        _boardManager.OnGameOver += OnGameOver;
    }

    private void OnStartGameButton()
    {
        _boardManager.StartGame();
        DisableStart_Restart();
    }
    private void OnRestartGameButton()
    {
        _boardManager.RestartGame();
        DisableStart_Restart();
    }
    private void OnGameOver()
    {
        _restartGameBtn.interactable = true;
        _startGameBtn.interactable = false;
    }
    private void DisableStart_Restart()
    {
        _startGameBtn.interactable = false;
        _restartGameBtn.interactable = false;
    }
}
