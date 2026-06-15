using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [SerializeField] Camera mainCamera;
    [SerializeField] RectTransform hotbarTransform;
    [SerializeField] TextMeshProUGUI txtLives;
    [SerializeField] TextMeshProUGUI txtScore;
    [SerializeField] RoadVisualizer roadVisualizer;

    public int lives = 3;

    public int PerfectDodges { get; private set; }

    [SerializeField] float firstExpansionTime = 60f;
    [SerializeField] float secondExpansionTime = 120f;
    [SerializeField] float maxDifficultyTime = 180f;

    public float FirstExpansionTime => firstExpansionTime;
    public float SecondExpansionTime => secondExpansionTime;
    public float MaxDifficultyTime => maxDifficultyTime;

    public float Score { get; private set; }
    public int HighScore { get; private set; }
    public float currentSpeed = 5f;
    public float maxSpeed = 20f;
    public float acceleration = 0.05f;

    public int allowedLaneRange { get; private set; } = 1;

    [SerializeField] float baseOrthoSize = 2.8f;
    [SerializeField] float zoomSpeed = 2.5f;

    float targetOrthoSize;
    Vector3 targetCameraPos;
    public bool IsCameraZooming { get; private set; } = false;
    [SerializeField] float zoomEpsilon = 0.02f;
    [SerializeField] float posEpsilon = 0.01f;

    bool gameOver;
    public bool IsGameOver => gameOver;
    public float GameTime { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (mainCamera == null) mainCamera = Camera.main;
        if (roadVisualizer == null) roadVisualizer = FindFirstObjectByType<RoadVisualizer>();

        if (mainCamera != null)
        {
            if (Mathf.Approximately(mainCamera.orthographicSize, 0f))
                mainCamera.orthographicSize = baseOrthoSize;

            targetOrthoSize = mainCamera.orthographicSize;
            Vector3 p = mainCamera.transform.position;
            p.y = 0f;
            mainCamera.transform.position = p;
            targetCameraPos = mainCamera.transform.position;
        }
    }

    void Start()
    {
        HighScore = PlayerPrefs.GetInt("HighScore", 0);
        UpdateCameraForRange(allowedLaneRange, instant: true);
        UpdateUI();
    }

    void Update()
    {
        if (gameOver) return;
        GameTime += Time.deltaTime;
        IncreaseSpeed();
        IncreaseScore();
        HandleProgression();
        ApplySmoothCameraTracking();
        UpdateUI();
    }

    void IncreaseSpeed()
    {
        currentSpeed += acceleration * Time.deltaTime;
        currentSpeed = Mathf.Min(currentSpeed, maxSpeed);
    }

    void IncreaseScore()
    {
        Score += currentSpeed * Time.deltaTime;
    }

    public void AddPerfectDodge()
    {
        PerfectDodges++;
    }

    void HandleProgression()
    {
        if (GameTime >= firstExpansionTime && allowedLaneRange == 1 && !IsCameraZooming)
        {
            StartCoroutine(ExpandAndEnable(2));
        }
        else if (GameTime >= secondExpansionTime && allowedLaneRange == 2 && !IsCameraZooming)
        {
            StartCoroutine(ExpandAndEnable(3));
        }
    }

    IEnumerator ExpandAndEnable(int newRange)
    {
        if (IsCameraZooming) yield break;
        IsCameraZooming = true;
        UpdateCameraForRange(newRange, instant: false);
        while (mainCamera != null &&
               (Mathf.Abs(mainCamera.orthographicSize - targetOrthoSize) > zoomEpsilon ||
                Vector3.Distance(mainCamera.transform.position, targetCameraPos) > posEpsilon))
        {
            yield return null;
        }
        allowedLaneRange = newRange;
        IsCameraZooming = false;
    }

    void ApplySmoothCameraTracking()
    {
        if (mainCamera == null) return;
        mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, targetOrthoSize, zoomSpeed * Time.deltaTime);
        mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetCameraPos, zoomSpeed * Time.deltaTime);
    }

    void UpdateUI()
    {
        if (txtScore != null) txtScore.text = "Score - " + Mathf.FloorToInt(Score);
        if (txtLives != null) txtLives.text = "Lives - " + lives;
    }

    public void AddLife()
    {
        if (gameOver) return;
        lives++;
        UpdateUI();
    }

    public void AddScore(int amount)
    {
        if (gameOver) return;
        Score += amount;
        UpdateUI();
    }

    public void TakeDamage()
    {
        if (gameOver) return;
        lives--;
        UpdateUI();

        if (lives <= 0) GameOver();
    }

    public void GameOver()
    {
        gameOver = true;
        int finalScore = Mathf.FloorToInt(Score);
        HighScore = Mathf.Max(finalScore, HighScore);
        PlayerPrefs.SetInt("HighScore", HighScore);

        // Last run stats
        PlayerPrefs.SetInt("LastScore", finalScore);
        PlayerPrefs.SetFloat("LastSurvivalTime", GameTime);
        PlayerPrefs.SetInt("LastMaxLanes", allowedLaneRange * 2 + 1);
        PlayerPrefs.SetInt("LastPerfectDodges", PerfectDodges);

        PlayerPrefs.Save();
        Debug.Log("GAME OVER");

        if (HighScoreManager.IsTop10(finalScore))
        {
            // show popup
        }
        else
        {
            SceneManager.LoadScene("HighScoreScene");
        }
    }

    public float GetBaseOrthoSize() => baseOrthoSize;

    void UpdateCameraForRange(int range, bool instant)
    {
        if (roadVisualizer == null || mainCamera == null)
        {
            targetOrthoSize = baseOrthoSize;
            targetCameraPos = mainCamera != null ? mainCamera.transform.position : Vector3.zero;
            return;
        }

        float highestLane = range;
        float lowestLane = -range;
        float laneHeight = roadVisualizer.GetLaneHeight();
        float visibleTop = highestLane * roadVisualizer.GetLaneSpacing() + (laneHeight * 0.5f);
        float visibleBottom = lowestLane * roadVisualizer.GetLaneSpacing() - (laneHeight * 0.5f);

        float hotbarPixels = hotbarTransform != null ? hotbarTransform.rect.height : 0f;
        float f = hotbarPixels / (float)Screen.height;
        float worldSpan = visibleTop - visibleBottom;
        float ortho = worldSpan / (2f * (1f - f));
        ortho = Mathf.Max(0.1f, ortho);

        float camY = visibleBottom + ortho;

        targetOrthoSize = ortho;
        targetCameraPos = new Vector3(mainCamera.transform.position.x, camY, mainCamera.transform.position.z);

        if (instant)
        {
            mainCamera.orthographicSize = targetOrthoSize;
            mainCamera.transform.position = targetCameraPos;
        }
    }
}