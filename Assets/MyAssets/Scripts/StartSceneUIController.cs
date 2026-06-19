using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class StartSceneUIController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    private Button exitGameButton;
    private Button nextRunButton;
    private Button leaderboardButton;

    private Button[] buttons;
    private int currentIndex;

    private Action selectedAction;

    private Label scoreLabel;
    private Label survivalLabel;
    private Label lanesLabel;
    private Label ratingLabel;

    private Label star1;
    private Label star2;
    private Label star3;

    private void Awake()
    {
        VisualElement root = uiDocument.rootVisualElement;

        exitGameButton = root.Q<Button>("exit-game-button");
        nextRunButton = root.Q<Button>("next-run-button");
        leaderboardButton = root.Q<Button>("leaderboard-button");

        buttons = new[]
        {
            exitGameButton,
            nextRunButton,
            leaderboardButton
        };

        // Mouse clicks
        exitGameButton.clicked += ExitGame;
        nextRunButton.clicked += StartNewRun;
        leaderboardButton.clicked += OpenLeaderboard;

        // Hover selection
        RegisterHoverCallbacks();

        // Default selected button
        currentIndex = 0;
        SelectButton(currentIndex);

        scoreLabel = root.Q<Label>("score-label");
        survivalLabel = root.Q<Label>("survival-label");
        lanesLabel = root.Q<Label>("lanes-label");
        ratingLabel = root.Q<Label>("rating-label");

        star1 = root.Q<Label>("star1");
        star2 = root.Q<Label>("star2");
        star3 = root.Q<Label>("star3");

        LoadLastRun();
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            currentIndex--;

            if (currentIndex < 0)
                currentIndex = buttons.Length - 1;

            SelectButton(currentIndex);
        }

        if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            currentIndex++;

            if (currentIndex >= buttons.Length)
                currentIndex = 0;

            SelectButton(currentIndex);
        }

        if (Keyboard.current.leftCtrlKey.wasPressedThisFrame)
        {
            selectedAction?.Invoke();
        }
    }

    private void RegisterHoverCallbacks()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i;

            buttons[i].RegisterCallback<MouseEnterEvent>(_ =>
            {
                currentIndex = index;
                SelectButton(index);
            });
        }
    }

    private void SelectButton(int index)
    {
        foreach (Button button in buttons)
        {
            button.RemoveFromClassList("selected");
        }

        buttons[index].AddToClassList("selected");

        // Assign action for LeftCtrl key
        if (buttons[index] == exitGameButton)
            selectedAction = ExitGame;
        else if (buttons[index] == nextRunButton)
            selectedAction = StartNewRun;
        else if (buttons[index] == leaderboardButton)
            selectedAction = OpenLeaderboard;
    }

    private void StartNewRun()
    {
        SceneManager.LoadScene("GameScene");
    }

    private void OpenLeaderboard()
    {
        SceneManager.LoadScene("HighScoreScene");
    }

    private void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void LoadLastRun()
    {
        int score = PlayerPrefs.GetInt("LastScore", 0);
        float time = PlayerPrefs.GetFloat("LastSurvivalTime", 0);
        int lanes = PlayerPrefs.GetInt("LastMaxLanes", 3);

        scoreLabel.text = score.ToString();
        lanesLabel.text = lanes.ToString();

        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);

        survivalLabel.text = $"{minutes:00}:{seconds:00}";

        UpdateRating(score);
    }

    private void UpdateRating(int score)
    {
        if (score >= 10000)
        {
            ratingLabel.text = "EXCELLENT";
            star1.text = "★";
            star2.text = "★";
            star3.text = "★";
        }
        else if (score >= 5000)
        {
            ratingLabel.text = "GOOD";
            star1.text = "★";
            star2.text = "★";
            star3.text = "☆";
        }
        else
        {
            ratingLabel.text = "TRY AGAIN";
            star1.text = "★";
            star2.text = "☆";
            star3.text = "☆";
        }
    }
}