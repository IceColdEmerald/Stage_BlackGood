using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    public enum ObstacleType { Standard, Car, Heart }

    [Header("Unified Warning Setup")]
    [SerializeField] GameObject carDangerPrefab; 
    [SerializeField] GameObject bigWarningPrefab; 
    
    [SerializeField] string dangerIconChildName = "Danger_Bubble_Icon";
    [SerializeField] string heartIconChildName = "Health_Bubble_Icon";
    [SerializeField] float flickerSpeed = 0.15f;

    [Header("Obstacle Prefabs")]
    [SerializeField] GameObject obstaclePrefab;
    [SerializeField] GameObject carObstaclePrefab;
    [SerializeField] GameObject heartObstaclePrefab;

    [Header("Spawn Probabilities")]
    [SerializeField] [Range(0f, 1f)] float carSpawnChance = 0.35f;
    [SerializeField] [Range(0f, 1f)] float heartSpawnChance = 0.15f;

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
    
    float spawnTimer;
    RoadVisualizer roadVisualizer;

    void Start()
    {
        roadVisualizer = FindFirstObjectByType<RoadVisualizer>();
        spawnTimer = startSpawnInterval;
    }

    void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.IsGameOver) return;

        float progress = Mathf.Clamp01(GameManager.Instance.GameTime / GameManager.Instance.MaxDifficultyTime);

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0)
        {
            float currentWarningDuration = Mathf.Lerp(startWarningDuration, endWarningDuration, progress);
            StartCoroutine(SpawnMultiLaneWaves(progress, currentWarningDuration));
            
            float currentSpawnInterval = Mathf.Lerp(startSpawnInterval, endSpawnInterval, progress);
            spawnTimer = currentSpawnInterval * Random.Range(minIntervalVariance, maxIntervalVariance);
        }
    }

    IEnumerator SpawnMultiLaneWaves(float progress, float warningDuration)
    {
        if (roadVisualizer == null) yield break;

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

        for (int i = 0; i < totalLanes; i++)
        {
            float currentSpawnChance = Mathf.Lerp(0.4f, 0.8f, progress);
            
            if (Random.value < currentSpawnChance) 
            {
                laneHasSpawn[i] = true;
                waveIsEmpty = false;
                
                float typeRoll = Random.value;
                if (typeRoll < carSpawnChance) laneTypes[i] = ObstacleType.Car;
                else if (typeRoll < carSpawnChance + heartSpawnChance) laneTypes[i] = ObstacleType.Heart;
                else laneTypes[i] = ObstacleType.Standard;

                GenerateContinuousDifficultyColor(progress, out laneColors[i], out lanePassable[i]);
                carMovesRight[i] = (laneTypes[i] == ObstacleType.Car && progress > 0.3f) ? (Random.value < 0.5f) : false;

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
            GenerateContinuousDifficultyColor(progress, out laneColors[forcedIndex], out lanePassable[forcedIndex]);
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

        float currentFlickerChance = Mathf.Lerp(0.1f, 0.85f, progress); 

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
                bool bubbleFlickers;

                if (laneTypes[i] == ObstacleType.Standard)
                {
                    bubbleFlickers = Random.value < currentFlickerChance;
                }
                else
                {
                    bubbleFlickers = iconFlickers && (Random.value < currentFlickerChance);
                }

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
                    SpriteRenderer bubbleRenderer = warning.GetComponent<SpriteRenderer>();
                    SpriteRenderer dangerRenderer = dangerIcon != null ? dangerIcon.GetComponent<SpriteRenderer>() : null;
                    SpriteRenderer heartRenderer = heartIcon != null ? heartIcon.GetComponent<SpriteRenderer>() : null;

                    if (laneTypes[i] == ObstacleType.Standard)
                    {
                        if (dangerIcon != null) dangerIcon.gameObject.SetActive(false);
                        if (heartIcon != null) heartIcon.gameObject.SetActive(false);
                        if (bubbleRenderer != null) 
                        {
                            bubbleRenderer.color = laneColors[i]; 
                            if (bubbleFlickers) flickeringIcons.Add(bubbleRenderer);
                        }
                    }
                    else if (laneTypes[i] == ObstacleType.Car)
                    {
                        if (heartIcon != null) heartIcon.gameObject.SetActive(false);
                        if (dangerIcon != null) dangerIcon.gameObject.SetActive(true);
                        if (bubbleRenderer != null) 
                        {
                            bubbleRenderer.color = Color.black; 
                            if (bubbleFlickers) flickeringIcons.Add(bubbleRenderer);
                        }
                        if (dangerRenderer != null) 
                        {
                            dangerRenderer.color = laneColors[i]; 
                            if (iconFlickers) flickeringIcons.Add(dangerRenderer);
                        }
                    }
                    else if (laneTypes[i] == ObstacleType.Heart)
                    {
                        if (dangerIcon != null) dangerIcon.gameObject.SetActive(false);
                        if (heartIcon != null) heartIcon.gameObject.SetActive(true);
                        if (bubbleRenderer != null) 
                        {
                            bubbleRenderer.color = Color.black; 
                            if (bubbleFlickers) flickeringIcons.Add(bubbleRenderer);
                        }
                        if (heartRenderer != null) 
                        {
                            heartRenderer.color = laneColors[i]; 
                            if (iconFlickers) flickeringIcons.Add(heartRenderer);
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
            if (GameManager.Instance.IsGameOver) break;
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

        if (GameManager.Instance.IsGameOver) yield break;

        for (int i = 0; i < totalLanes; i++)
        {
            if (!laneHasSpawn[i]) continue;
            float spawnY = (minLane + i) * roadVisualizer.GetLaneSpacing();
            GameObject prefabToSpawn = obstaclePrefab;
            List<GameObject> targetPool = obstaclePool;

            if (laneTypes[i] == ObstacleType.Car) { prefabToSpawn = carObstaclePrefab; targetPool = carObstaclePool; }
            else if (laneTypes[i] == ObstacleType.Heart) { prefabToSpawn = heartObstaclePrefab; targetPool = heartObstaclePool; }

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

    void GenerateContinuousDifficultyColor(float progress, out Color color, out bool isPassable)
    {
        if (Random.value < 0.3f) { isPassable = true; color = Color.black; return; }
        isPassable = false;
        Color[] vividColors = { Color.red, Color.green, Color.yellow, Color.magenta, Color.cyan };
        Color chosenColor = vividColors[Random.Range(0, vividColors.Length)];
        if (progress < 0.33f) color = chosenColor;
        else if (progress < 0.66f) color = Color.Lerp(chosenColor, Color.gray, (progress - 0.33f) / 0.33f);
        else color = Color.Lerp(Color.gray, new Color(0.2f, 0.2f, 0.2f, 1f), (progress - 0.66f) / 0.34f);
    }

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