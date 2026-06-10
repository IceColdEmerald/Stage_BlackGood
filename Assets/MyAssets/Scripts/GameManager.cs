using System.Collections;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private RectTransform hotbarTransform;
    [SerializeField] private TextMeshProUGUI txtLives;
    [SerializeField] private TextMeshProUGUI txtScore;
    [SerializeField] private RoadVisualizer roadVisualizer;

    [Header("Player Stats")]
    public int lives = 3;

    [Header("Difficulty Scaling")]
    [SerializeField] int firstExpansion = 500;
    [SerializeField] int secondExpansion = 1500;

    [Header("Score & Speed")]
    public float Score { get; private set; }
    public int HighScore { get; private set; }
    public float currentSpeed = 5f;
    public float maxSpeed = 20f;
    public float acceleration = 0.05f;

    [Header("Progression Bounds")]
    public int allowedLaneRange { get; private set; } = 1;

    [Header("Camera Scaling Configuration")]
    [SerializeField] private float baseOrthoSize = 2.8f;
    [SerializeField] private float zoomSpeed = 2.5f;

    private float targetOrthoSize;
    private Vector3 targetCameraPos;
    private bool gameOver;
    public bool IsCameraZooming { get; private set; } = false;
    [SerializeField] private float zoomEpsilon = 0.02f;
    [SerializeField] private float posEpsilon = 0.01f;

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

    void HandleProgression()
    {
        if (Score >= firstExpansion && allowedLaneRange == 1 && !IsCameraZooming)
        {
            StartCoroutine(ExpandAndEnable(2));
        }
        else if (Score >= secondExpansion && allowedLaneRange == 2 && !IsCameraZooming)
        {
            StartCoroutine(ExpandAndEnable(3));
        }
    }

    private IEnumerator ExpandAndEnable(int newRange)
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

    public void GameOver()
    {
        gameOver = true;
        int finalScore = Mathf.FloorToInt(Score);
        HighScore = Mathf.Max(finalScore, HighScore);
        PlayerPrefs.SetInt("HighScore", HighScore);
        PlayerPrefs.Save();
        Debug.Log("GAME OVER");
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
