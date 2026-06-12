using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    public enum ObstacleType { Standard, Car, Heart, ColorUpgrade, FlashUpgrade, SpawnUpgrade }

    [Header("Unified Warning Setup")]
    [SerializeField] GameObject carDangerPrefab; 
    [SerializeField] GameObject bigWarningPrefab; 
    
    [SerializeField] string dangerIconChildName = "Danger_Bubble_Icon";
    [SerializeField] string heartIconChildName = "Health_Bubble_Icon";
    [SerializeField] string colorIconChildName = "Color_Bubble_Icon";
    [SerializeField] string flashIconChildName = "Flash_Bubble_Icon";
    [SerializeField] string spawnIconChildName = "Spawn_Bubble_Icon";
    [SerializeField] float flickerSpeed = 0.15f;

    [Header("Obstacle Prefabs")]
    [SerializeField] GameObject obstaclePrefab;
    [SerializeField] GameObject carObstaclePrefab;
    [SerializeField] GameObject heartObstaclePrefab;
    [SerializeField] GameObject colorUpgradePrefab;
    [SerializeField] GameObject flashUpgradePrefab;
    [SerializeField] GameObject spawnUpgradePrefab;

    [Header("Spawn Probabilities")]
    [SerializeField] [Range(0f, 1f)] float carSpawnChance = 0.35f;
    [SerializeField] [Range(0f, 1f)] float upgradeSpawnChance = 0.15f; 

    [Header("Dynamic Pacing Thresholds")]
    [SerializeField] float startSpawnInterval = 2.5f;
    [SerializeField] float endSpawnInterval = 1.0f;
    [SerializeField] float startWarningDuration = 2.0f;
    [SerializeField] float endWarningDuration = 0.7f;
    [SerializeField] float spawnXOffset = 1f;
    [SerializeField] float minIntervalVariance = 0.75f;
    [SerializeField] float maxIntervalVariance = 1.25f;

    List<GameObject> warningPool = new List<GameObject>();
    List<GameObject> bigWarningPool = new List<GameObject>();
    List<GameObject> obstaclePool = new List<GameObject>();
    List<GameObject> carObstaclePool = new List<GameObject>();
    List<GameObject> heartObstaclePool = new List<GameObject>();
    List<GameObject> colorUpgradePool = new List<GameObject>();
    List<GameObject> flashUpgradePool = new List<GameObject>();
    List<GameObject> spawnUpgradePool = new List<GameObject>();
    
    float spawnTimer;
    RoadVisualizer roadVisualizer;

    // Decoupled difficulty parameters
    private float spawnProgress = 0f;
    private float colorProgress = 0f;
    private float flickerProgress = 0f;

    void Start()
    {
        roadVisualizer = FindFirstObjectByType<RoadVisualizer>();
        spawnTimer = startSpawnInterval;
    }

    void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.IsGameOver) return;

        float maxDifficultyTime = GameManager.Instance.MaxDifficultyTime > 0 ? GameManager.Instance.MaxDifficultyTime : 180f;

        spawnProgress = Mathf.Clamp01(spawnProgress + (Time.deltaTime / maxDifficultyTime));
        colorProgress = Mathf.Clamp01(colorProgress + (Time.deltaTime / maxDifficultyTime));
        flickerProgress = Mathf.Clamp01(flickerProgress + (Time.deltaTime / maxDifficultyTime));

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0)
        {
            float currentWarningDuration = Mathf.Lerp(startWarningDuration, endWarningDuration, spawnProgress);
            StartCoroutine(SpawnMultiLaneWaves(currentWarningDuration));
            
            float currentSpawnInterval = Mathf.Lerp(startSpawnInterval, endSpawnInterval, spawnProgress);
            spawnTimer = currentSpawnInterval * Random.Range(minIntervalVariance, maxIntervalVariance);
        }
    }

    IEnumerator SpawnMultiLaneWaves(float warningDuration)
    {
        if (roadVisualizer == null || GameManager.Instance == null) yield break;

        int currentRange = GameManager.Instance.allowedLaneRange;
        int minLane = -currentRange;
        int maxLane = currentRange;
        int totalLanes = maxLane - minLane + 1;

        bool[] laneHasSpawn = new bool[totalLanes];
        ObstacleType[] laneTypes = new ObstacleType[totalLanes];
        Color[] laneColors = new Color[totalLanes];
        bool[] lanePassable = new bool[totalLanes];
        bool[] carMovesRight = new bool[totalLanes];

        bool atLeastOneSafePath = false;
        bool waveIsEmpty = true;

        float currentSpawnChance = Mathf.Lerp(0.4f, 0.8f, spawnProgress);

        for (int i = 0; i < totalLanes; i++)
        {
            if (Random.value < currentSpawnChance) 
            {
                laneHasSpawn[i] = true;
                waveIsEmpty = false;
                
                float typeRoll = Random.value;
                if (typeRoll < carSpawnChance) 
                {
                    laneTypes[i] = ObstacleType.Car;
                }
                else if (typeRoll < carSpawnChance + upgradeSpawnChance) 
                {
                    float upgradePoolRoll = Random.value;
                    if (upgradePoolRoll < 0.25f) laneTypes[i] = ObstacleType.Heart;
                    else if (upgradePoolRoll < 0.50f) laneTypes[i] = ObstacleType.ColorUpgrade;
                    else if (upgradePoolRoll < 0.75f) laneTypes[i] = ObstacleType.FlashUpgrade;
                    else laneTypes[i] = ObstacleType.SpawnUpgrade;
                }
                else 
                {
                    laneTypes[i] = ObstacleType.Standard;
                }

                GenerateContinuousDifficultyColor(colorProgress, out laneColors[i], out lanePassable[i]);
                carMovesRight[i] = (laneTypes[i] == ObstacleType.Car && spawnProgress > 0.3f) ? (Random.value < 0.5f) : false;

                if (lanePassable[i]) atLeastOneSafePath = true;
            }
            else
            {
                atLeastOneSafePath = true;
            }
        }

        if (waveIsEmpty)
        {
            int forcedIndex = Random.Range(0, totalLanes);
            laneHasSpawn[forcedIndex] = true;
            laneTypes[forcedIndex] = ObstacleType.Standard;
            GenerateContinuousDifficultyColor(colorProgress, out laneColors[forcedIndex], out lanePassable[forcedIndex]);
            if (lanePassable[forcedIndex]) atLeastOneSafePath = true;
        }

        if (!atLeastOneSafePath)
        {
            int escapeIndex = Random.Range(0, totalLanes);
            laneHasSpawn[escapeIndex] = true;
            laneColors[escapeIndex] = Color.black;
            lanePassable[escapeIndex] = true;
        }

        List<GameObject> activeWarnings = new List<GameObject>();
        List<SpriteRenderer> flickeringIcons = new List<SpriteRenderer>();

        float currentFlickerChance = Mathf.Lerp(0.1f, 0.85f, flickerProgress); 

        for (int i = 0; i < totalLanes; i++)
        {
            if (!laneHasSpawn[i]) continue;

            float spawnY = (minLane + i) * roadVisualizer.GetLaneSpacing();
            GameObject warning = null;
            bool useBigWarning = laneTypes[i] == ObstacleType.Standard && bigWarningPrefab != null && Random.value > 0.5f;

            if (useBigWarning) warning = GetPooledObject(bigWarningPool, bigWarningPrefab);
            else warning = GetPooledObject(warningPool, carDangerPrefab);

            if (warning != null)
            {
                PositionAtScreenEdge(warning, spawnY, carMovesRight[i] ? -1 : 1, isObject: false);
                Vector3 origScale = warning.transform.localScale;
                float flipDirection = carMovesRight[i] ? -1f : 1f;
                warning.transform.localScale = new Vector3(Mathf.Abs(origScale.x) * flipDirection, origScale.y, origScale.z);

                bool iconFlickers = Random.value < currentFlickerChance;
                bool bubbleFlickers = (laneTypes[i] == ObstacleType.Standard) ? (Random.value < currentFlickerChance) : (iconFlickers && (Random.value < currentFlickerChance));

                if (useBigWarning)
                {
                    SpriteRenderer warningRenderer = warning.GetComponent<SpriteRenderer>();
                    if (warningRenderer != null)
                    {
                        warningRenderer.color = laneColors[i];
                        if (bubbleFlickers) flickeringIcons.Add(warningRenderer);
                    }
                }
                else
                {
                    Transform dangerIcon = warning.transform.Find(dangerIconChildName);
                    Transform heartIcon = warning.transform.Find(heartIconChildName);
                    Transform colorIcon = warning.transform.Find(colorIconChildName);
                    Transform flashIcon = warning.transform.Find(flashIconChildName);
                    Transform spawnIcon = warning.transform.Find(spawnIconChildName);

                    if (dangerIcon != null) dangerIcon.gameObject.SetActive(false);
                    if (heartIcon != null) heartIcon.gameObject.SetActive(false);
                    if (colorIcon != null) colorIcon.gameObject.SetActive(false);
                    if (flashIcon != null) flashIcon.gameObject.SetActive(false);
                    if (spawnIcon != null) spawnIcon.gameObject.SetActive(false);

                    SpriteRenderer bubbleRenderer = warning.GetComponent<SpriteRenderer>();

                    if (laneTypes[i] == ObstacleType.Standard)
                    {
                        if (bubbleRenderer != null) 
                        {
                            bubbleRenderer.color = laneColors[i]; 
                            if (bubbleFlickers) flickeringIcons.Add(bubbleRenderer);
                        }
                    }
                    else
                    {
                        if (bubbleRenderer != null) 
                        {
                            bubbleRenderer.color = Color.black; 
                            if (bubbleFlickers) flickeringIcons.Add(bubbleRenderer);
                        }

                        Transform targetIcon = null;
                        if (laneTypes[i] == ObstacleType.Car) targetIcon = dangerIcon;
                        else if (laneTypes[i] == ObstacleType.Heart) targetIcon = heartIcon;
                        else if (laneTypes[i] == ObstacleType.ColorUpgrade) targetIcon = colorIcon;
                        else if (laneTypes[i] == ObstacleType.FlashUpgrade) targetIcon = flashIcon;
                        else if (laneTypes[i] == ObstacleType.SpawnUpgrade) targetIcon = spawnIcon;

                        if (targetIcon != null)
                        {
                            targetIcon.gameObject.SetActive(true);
                            SpriteRenderer iconRenderer = targetIcon.GetComponent<SpriteRenderer>();
                            if (iconRenderer != null)
                            {
                                iconRenderer.color = laneColors[i]; 
                                if (iconFlickers) flickeringIcons.Add(iconRenderer);
                            }
                        }
                    }
                }
                warning.SetActive(true);
                activeWarnings.Add(warning);
            }
        }

        float timer = 0f;
        float flashTimer = 0f;
        bool isIconVisible = true;

        while (timer < warningDuration)
        {
            if (GameManager.Instance == null || GameManager.Instance.IsGameOver) break;
            timer += Time.deltaTime;
            flashTimer += Time.deltaTime;

            if (flashTimer >= flickerSpeed)
            {
                flashTimer = 0f;
                isIconVisible = !isIconVisible;
                foreach (SpriteRenderer sr in flickeringIcons) if (sr != null) sr.enabled = isIconVisible;
            }
            yield return null;
        }

        foreach (var w in activeWarnings) if (w != null) w.SetActive(false);
        foreach (SpriteRenderer sr in flickeringIcons) if (sr != null) sr.enabled = true;

        if (GameManager.Instance == null || GameManager.Instance.IsGameOver) yield break;

        for (int i = 0; i < totalLanes; i++)
        {
            if (!laneHasSpawn[i]) continue;
            float spawnY = (minLane + i) * roadVisualizer.GetLaneSpacing();
            GameObject prefabToSpawn = obstaclePrefab;
            List<GameObject> targetPool = obstaclePool;

            if (laneTypes[i] == ObstacleType.Car) { prefabToSpawn = carObstaclePrefab; targetPool = carObstaclePool; }
            else if (laneTypes[i] == ObstacleType.Heart) { prefabToSpawn = heartObstaclePrefab; targetPool = heartObstaclePool; }
            else if (laneTypes[i] == ObstacleType.ColorUpgrade) { prefabToSpawn = colorUpgradePrefab; targetPool = colorUpgradePool; }
            else if (laneTypes[i] == ObstacleType.FlashUpgrade) { prefabToSpawn = flashUpgradePrefab; targetPool = flashUpgradePool; }
            else if (laneTypes[i] == ObstacleType.SpawnUpgrade) { prefabToSpawn = spawnUpgradePrefab; targetPool = spawnUpgradePool; }

            GameObject obj = GetPooledObject(targetPool, prefabToSpawn);
            if (obj != null)
            {
                PositionAtScreenEdge(obj, spawnY, carMovesRight[i] ? -1 : 1, isObject: true);
                BaseObstacle obsComp = obj.GetComponent<BaseObstacle>();
                if (obsComp != null) obsComp.Setup(laneTypes[i], laneColors[i], lanePassable[i], carMovesRight[i]);
                obj.SetActive(true);
            }
        }
    }

    void GenerateContinuousDifficultyColor(float progressionFactor, out Color color, out bool isPassable)
    {
        if (Random.value < 0.3f) { isPassable = true; color = Color.black; return; }
        isPassable = false;
        Color[] vividColors = { Color.red, Color.green, Color.yellow, Color.magenta, Color.cyan };
        Color chosenColor = vividColors[Random.Range(0, vividColors.Length)];
        if (progressionFactor < 0.33f) color = chosenColor;
        else if (progressionFactor < 0.66f) color = Color.Lerp(chosenColor, Color.gray, (progressionFactor - 0.33f) / 0.33f);
        else color = Color.Lerp(Color.gray, new Color(0.2f, 0.2f, 0.2f, 1f), (progressionFactor - 0.66f) / 0.34f);
    }

    public void RollbackColorDifficulty(float amount) => colorProgress = Mathf.Max(0f, colorProgress - amount);
    public void RollbackFlickerDifficulty(float amount) => flickerProgress = Mathf.Max(0f, flickerProgress - amount);
    public void RollbackSpawnDifficulty(float amount) => spawnProgress = Mathf.Max(0f, spawnProgress - amount);

    void PositionAtScreenEdge(GameObject obj, float yPos, int sideSign, bool isObject)
    {
        if (obj == null || Camera.main == null) return;
        float halfWidth = Camera.main.orthographicSize * Camera.main.aspect;
        float screenEdgeX = Camera.main.transform.position.x + (halfWidth * sideSign);
        float finalX = isObject ? screenEdgeX + (spawnXOffset * sideSign * 2f) : screenEdgeX - (spawnXOffset * sideSign);
        obj.transform.position = new Vector3(finalX, yPos, 0f);
    }

    GameObject GetPooledObject(List<GameObject> pool, GameObject prefab)
    {
        if (prefab == null) return null;
        foreach (GameObject obj in pool) if (obj != null && !obj.activeInHierarchy) return obj;
        GameObject newObj = Instantiate(prefab, transform);
        pool.Add(newObj);
        return newObj;
    }
}