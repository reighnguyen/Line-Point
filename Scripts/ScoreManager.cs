using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    class PlayerInformation
    {
        public int score;
    }

    [SerializeField]
    private string _scoreLabel;
    [SerializeField]
    private TextMeshProUGUI _scoreText;
    private PlayerInformation _player;

    private void Awake()
    {
        _player = new PlayerInformation();
    }

    public void InscreaseScore(int plusBy)
    {
        _player.score += plusBy;

        _scoreText.text = string.Format("{0} {1}", _scoreLabel, _player.score.ToString());
    }
}
