using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class HighScoreSceneController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    private Button playAgainButton;
    private Button backButton;
    private int currentIndex;

    private Button[] buttons;
    private Action[] buttonActions;

    private void Start()
    {
        List<HighScoreEntry> scores = HighScoreManager.LoadScores();

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

        for (int i = 0; i < 10; i++)
        {
            VisualElement row = root.Q<VisualElement>($"entry-{i + 1}");

            Label rank = row.Q<Label>(className: "rank");
            Label player = row.Q<Label>(className: "player");
            Label score = row.Q<Label>(className: "score");

            if (i < scores.Count)
            {
                rank.text = (i + 1).ToString();

                player.text =
                    $"{scores[i].playerName}  " +
                    $"({FormatTime(scores[i].survivalTime)} | {scores[i].maxLanes} lanes)";

                score.text = scores[i].score.ToString();
            }
            else
            {
                rank.text = (i + 1).ToString();
                player.text = "---";
                score.text = "---";
            }
        }
    }

    private void Update()
    {
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

        if (Keyboard.current.enterKey.wasPressedThisFrame)
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

    void OnPlayAgain()
    {
        Debug.Log("Play Again");
        SceneManager.LoadScene("GameScene");
    }

    void OnBack()
    {
        Debug.Log("Back to Main Menu");
        SceneManager.LoadScene("StartScene");
    }

    private string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);

        return $"{minutes:00}:{secs:00}";
    }
}