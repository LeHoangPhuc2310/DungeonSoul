using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    [Header("Results")]
    [SerializeField] private TMP_Text scoreResultText;
    [SerializeField] private TMP_Text floorResultText;
    [SerializeField] private TMP_Text coinsResultText;

    [Header("Buttons")]
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button menuButton;

    private void Awake()
    {
        if (playAgainButton != null)
            playAgainButton.onClick.AddListener(PlayAgain);
        if (menuButton != null)
            menuButton.onClick.AddListener(GoToMenu);
    }

    public void Setup(int score, int floor, int coins)
    {
        if (scoreResultText != null) scoreResultText.text = "Score: " + score;
        if (floorResultText != null) floorResultText.text = "Floor: " + floor;
        if (coinsResultText != null) coinsResultText.text = "Coins: " + coins;
    }

    public void PlayAgain()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMenu()
    {
        Time.timeScale = 1f;
        // Assuming there is a "Menu" scene or just reloading current for now if not specified
        SceneManager.LoadScene(0); 
    }
}
