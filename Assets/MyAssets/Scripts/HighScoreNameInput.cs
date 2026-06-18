using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class HighScoreNameInput : MonoBehaviour
{
    public static HighScoreNameInput Instance;

    [SerializeField] private UIDocument uiDocument;

    private VisualElement popup;
    private Label currentName;

    private char[] letters = new char[3] { 'A', 'A', 'A' };
    private int currentIndex;
    private int score;

    private bool isActive;

    void Awake()
    {
        Instance = this;

        var root = uiDocument.rootVisualElement;

        popup = root.Q<VisualElement>("NameInputPopup");
        currentName = root.Q<Label>("CurrentName");

        popup.style.display = DisplayStyle.None;
    }

    public void Open(int finalScore)
    {
        score = finalScore;
        currentIndex = 0;

        letters[0] = 'A';
        letters[1] = 'A';
        letters[2] = 'A';

        UpdateName();

        popup.style.display = DisplayStyle.Flex;
        isActive = true;
    }

    void Update()
    {
        if (!isActive) return;

        HandleInput();
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            letters[currentIndex]++;
            if (letters[currentIndex] > 'Z')
                letters[currentIndex] = 'A';

            UpdateName();
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            letters[currentIndex]--;
            if (letters[currentIndex] < 'A')
                letters[currentIndex] = 'Z';

            UpdateName();
        }

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            currentIndex++;

            if (currentIndex > 2)
            {
                SaveScore();
            }
        }
    }

    void UpdateName()
    {
        currentName.text = $"{letters[0]} {letters[1]} {letters[2]}";
    }

    void SaveScore()
    {
        string playerName = new string(letters);

        HighScoreManager.Instance.AddPoints(playerName, score);

        isActive = false;
        popup.style.display = DisplayStyle.None;

        SceneManager.LoadScene("HighScoreScene");
    }
}