using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class HighScoreSceneController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    private Button playAgainButton;
    private Button backButton;

    private Button[] buttons;
    private Action[] buttonActions;

    private int currentIndex;

    private void Start()
    {
        VisualElement root = uiDocument.rootVisualElement;

        playAgainButton = root.Q<Button>("play-button");
        backButton = root.Q<Button>("back-button");

        buttons = new[] { playAgainButton, backButton };
        buttonActions = new Action[] { OnPlayAgain, OnBack };

        playAgainButton.clicked += OnPlayAgain;
        backButton.clicked += OnBack;

        RegisterHoverCallbacks();

        currentIndex = 0;
        SelectButton(currentIndex);

        List<HighScoreEntry> scores = new();

        if (HighScoreManager.Instance != null)
        {
            scores = HighScoreManager.Instance.LoadScores();
        }
        else
        {
            Debug.LogError("HighScoreManager.Instance is NULL!");
        }

        for (int i = 0; i < 10; i++)
        {
            VisualElement row = root.Q<VisualElement>($"entry-{i + 1}");

            if (row == null)
            {
                Debug.LogWarning($"Missing entry-{i + 1}");
                continue;
            }

            Label rank = row.Q<Label>(className: "rank");
            Label player = row.Q<Label>(className: "player");
            Label score = row.Q<Label>(className: "score");

            if (rank == null || player == null || score == null)
            {
                Debug.LogWarning($"Missing labels in entry-{i + 1}");
                continue;
            }

            rank.text = (i + 1).ToString();

            if (i < scores.Count)
            {
                player.text =
                    $"{scores[i].playerName} ({FormatTime(scores[i].survivalTime)} | {scores[i].maxLanes} lanes)";

                score.text = scores[i].score.ToString();
            }
            else
            {
                player.text = "---";
                score.text = "---";
            }
        }
    }

    private void Update()
    {
        if (buttons == null)
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
            buttonActions[currentIndex]?.Invoke();
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
    }

    private void OnPlayAgain()
    {
        SceneManager.LoadScene("GameScene");
    }

    private void OnBack()
    {
        SceneManager.LoadScene("StartScene");
    }

    private string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);

        return $"{minutes:00}:{secs:00}";
    }
}