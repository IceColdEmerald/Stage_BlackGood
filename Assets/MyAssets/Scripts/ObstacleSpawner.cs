using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    enum ObstacleType { Standard, Car }

    [Header("Standard Hazard Setup")]
    [SerializeField] GameObject obstaclePrefab;
    [SerializeField] GameObject[] warningPrefabs;

    [Header("Moving Car Setup")]
    [SerializeField] GameObject carObstaclePrefab;
    [SerializeField] GameObject carWarningPrefab; 
    [SerializeField] string carIconChildName = "Danger_Bubble_Icon"; 
    [SerializeField] float carSpawnChance = 0.4f; 

    [Header("Dynamic Pacing Thresholds")]
    [SerializeField] float startSpawnInterval = 2.5f;
    [SerializeField] float endSpawnInterval = 1.0f;
    [SerializeField] float startWarningDuration = 2.0f;
    [SerializeField] float endWarningDuration = 0.7f;
    [SerializeField] float spawnXOffset = 1f;
    [SerializeField] float minIntervalVariance = 0.75f;
    [SerializeField] float maxIntervalVariance = 1.25f;

    List<GameObject> obstaclePool = new List<GameObject>();
    List<GameObject>[] warningPools;
    List<GameObject> carObstaclePool = new List<GameObject>();
    List<GameObject> carWarningPool = new List<GameObject>();
    
    float spawnTimer;
    RoadVisualizer roadVisualizer;

    void Start()
    {
        roadVisualizer = FindFirstObjectByType<RoadVisualizer>();
        spawnTimer = startSpawnInterval;

        if (warningPrefabs != null)
        {
            warningPools = new List<GameObject>[warningPrefabs.Length];
            for (int i = 0; i < warningPrefabs.Length; i++)
            {
                warningPools[i] = new List<GameObject>();
            }
        }
        else
        {
            warningPools = new List<GameObject>[0];
        }
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
                laneTypes[i] = (Random.value < carSpawnChance) ? ObstacleType.Car : ObstacleType.Standard;
                GenerateContinuousDifficultyColor(progress, out laneColors[i], out lanePassable[i]);
                
                if (laneTypes[i] == ObstacleType.Car && progress > 0.3f)
                {
                    carMovesRight[i] = (Random.value < 0.5f);
                }
                else
                {
                    carMovesRight[i] = false;
                }

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
            laneTypes[forcedIndex] = (Random.value < carSpawnChance) ? ObstacleType.Car : ObstacleType.Standard;
            GenerateContinuousDifficultyColor(progress, out laneColors[forcedIndex], out lanePassable[forcedIndex]);
            carMovesRight[forcedIndex] = (laneTypes[forcedIndex] == ObstacleType.Car && progress > 0.3f) ? (Random.value < 0.5f) : false;
            if (lanePassable[forcedIndex]) atLeastOneSafePath = true;
        }

        if (!atLeastOneSafePath)
        {
            int escapeIndex = Random.Range(0, totalLanes);
            laneHasSpawn[escapeIndex] = true;
            laneColors[escapeIndex] = Color.black;
            lanePassable[escapeIndex] = true;
            carMovesRight[escapeIndex] = false;
        }

        List<GameObject> activeWarnings = new List<GameObject>();
        List<Coroutine> activeFlickers = new List<Coroutine>();

        float currentFlickerChance = progress * 0.5f;

        for (int i = 0; i < totalLanes; i++)
        {
            if (!laneHasSpawn[i]) continue;

            float spawnY = (minLane + i) * roadVisualizer.GetLaneSpacing();
            GameObject warning = null;

            if (laneTypes[i] == ObstacleType.Car)
            {
                warning = GetPooledObject(carWarningPool, carWarningPrefab);
                if (warning != null)
                {
                    PositionAtScreenEdge(warning, spawnY, carMovesRight[i] ? -1 : 1, isObject: false);
                    
                    // FLIP FIX: Maintain the exact scale size set on your prefab asset
                    Vector3 currentScale = warning.transform.localScale;
                    float flipDirection = carMovesRight[i] ? -1f : 1f;
                    warning.transform.localScale = new Vector3(Mathf.Abs(currentScale.x) * flipDirection, currentScale.y, currentScale.z);

                    Transform iconChild = warning.transform.Find(carIconChildName);
                    SpriteRenderer targetRenderer = iconChild != null ? iconChild.GetComponent<SpriteRenderer>() : warning.GetComponentInChildren<SpriteRenderer>();
                    
                    if (targetRenderer != null) targetRenderer.color = laneColors[i];
                }
            }
            else
            {
                if (warningPrefabs != null && warningPrefabs.Length > 0)
                {
                    int randomWarningIndex = Random.Range(0, warningPrefabs.Length);
                    warning = GetPooledStandardWarning(randomWarningIndex);
                    if (warning != null)
                    {
                        PositionAtScreenEdge(warning, spawnY, 1, isObject: false);
                        
                        // FLIP FIX: Keep the original prefab scale intact for standard hazards
                        Vector3 currentScale = warning.transform.localScale;
                        warning.transform.localScale = new Vector3(Mathf.Abs(currentScale.x), currentScale.y, currentScale.z);

                        SpriteRenderer sr = warning.GetComponent<SpriteRenderer>();
                        if (sr != null) sr.color = laneColors[i];
                    }
                }
            }

            if (warning != null)
            {
                SpriteRenderer[] renderers = warning.GetComponentsInChildren<SpriteRenderer>();
                foreach (var r in renderers) if (r != null) r.enabled = true;

                warning.SetActive(true);
                activeWarnings.Add(warning);

                if (Random.value < currentFlickerChance)
                {
                    activeFlickers.Add(StartCoroutine(TelegraphFlickerRoutine(warning)));
                }
            }
        }

        yield return new WaitForSeconds(warningDuration);

        foreach (var coroutine in activeFlickers) if (coroutine != null) StopCoroutine(coroutine);
        foreach (var w in activeWarnings) if (w != null) w.SetActive(false);

        if (GameManager.Instance.IsGameOver) yield break;

        for (int i = 0; i < totalLanes; i++)
        {
            if (!laneHasSpawn[i]) continue;

            float spawnY = (minLane + i) * roadVisualizer.GetLaneSpacing();
            
            if (laneTypes[i] == ObstacleType.Car)
            {
                GameObject car = GetPooledObject(carObstaclePool, carObstaclePrefab);
                if (car != null)
                {
                    PositionAtScreenEdge(car, spawnY, carMovesRight[i] ? -1 : 1, isObject: true);
                    CarObstacle carComp = car.GetComponent<CarObstacle>();
                    if (carComp != null) carComp.Setup(laneColors[i], lanePassable[i], carMovesRight[i]);
                    car.SetActive(true);
                }
            }
            else
            {
                GameObject obstacle = GetPooledObject(obstaclePool, obstaclePrefab);
                if (obstacle != null)
                {
                    PositionAtScreenEdge(obstacle, spawnY, 1, isObject: true);
                    Obstacle obsComp = obstacle.GetComponent<Obstacle>();
                    if (obsComp != null) obsComp.Setup(laneColors[i], lanePassable[i]);
                    obstacle.SetActive(true);
                }
            }
        }
    }

    IEnumerator TelegraphFlickerRoutine(GameObject obj)
    {
        if (obj == null) yield break;
        SpriteRenderer[] renderers = obj.GetComponentsInChildren<SpriteRenderer>();
        while (obj != null && obj.activeInHierarchy)
        {
            foreach (var r in renderers) if (r != null) r.enabled = !r.enabled;
            yield return new WaitForSeconds(0.12f);
        }
        if (renderers != null)
        {
            foreach (var r in renderers) if (r != null) r.enabled = true;
        }
    }

    void GenerateContinuousDifficultyColor(float progress, out Color color, out bool isPassable)
    {
        if (Random.value < 0.3f) 
        {
            isPassable = true;
            color = Color.black;
            return;
        }

        isPassable = false;
        Color[] vividColors = { Color.red, Color.green, Color.yellow, Color.magenta, Color.cyan };
        Color chosenColor = vividColors[Random.Range(0, vividColors.Length)];

        if (progress < 0.33f)
        {
            color = chosenColor;
        }
        else if (progress < 0.66f)
        {
            float shiftProgress = (progress - 0.33f) / 0.33f;
            color = Color.Lerp(chosenColor, Color.gray, shiftProgress);
        }
        else
        {
            float finalProgress = (progress - 0.66f) / 0.34f;
            float targetGrayValue = Random.Range(0.12f, 0.32f);
            Color endgameGray = new Color(targetGrayValue, targetGrayValue, targetGrayValue, 1f);
            
            Color midGameMuted = Color.Lerp(chosenColor, Color.gray, 1f);
            color = Color.Lerp(midGameMuted, endgameGray, finalProgress);
        }
    }

    void PositionAtScreenEdge(GameObject obj, float yPos, int sideSign, bool isObject)
    {
        if (obj == null || Camera.main == null) return;
        float halfWidth = Camera.main.orthographicSize * Camera.main.aspect;
        float screenEdgeX = Camera.main.transform.position.x + (halfWidth * sideSign);
        
        float finalX;
        if (isObject)
        {
            finalX = screenEdgeX + (spawnXOffset * sideSign * 2f);
        }
        else
        {
            finalX = screenEdgeX - (spawnXOffset * sideSign);
        }

        obj.transform.position = new Vector3(finalX, yPos, 0f);
    }

    GameObject GetPooledStandardWarning(int index)
    {
        if (warningPools == null || index < 0 || index >= warningPrefabs.Length) return null;
        if (warningPrefabs[index] == null) return null;
        
        foreach (GameObject obj in warningPools[index])
        {
            if (obj != null && !obj.activeInHierarchy) return obj;
        }
        
        GameObject newObj = Instantiate(warningPrefabs[index]);
        if (newObj != null)
        {
            newObj.transform.SetParent(transform);
            warningPools[index].Add(newObj);
        }
        return newObj;
    }

    GameObject GetPooledObject(List<GameObject> pool, GameObject prefab)
    {
        if (prefab == null || pool == null) return null;
        foreach (GameObject obj in pool)
        {
            if (obj != null && !obj.activeInHierarchy) return obj;
        }
        GameObject newObj = Instantiate(prefab);
        if (newObj != null)
        {
            newObj.transform.SetParent(transform);
            pool.Add(newObj);
        }
        return newObj;
    }
}