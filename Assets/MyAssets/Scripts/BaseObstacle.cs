using UnityEngine;

public class BaseObstacle : MonoBehaviour
{
    private ObstacleSpawner.ObstacleType obstacleType;
    private bool isPassable;
    private bool movesRight;
    private bool hasRewardedPoints = false;
    
    [Header("Chaos Settings")]
    [Tooltip("How much faster should cars move compared to normal obstacles?")]
    [SerializeField] float carSpeedMultiplier = 1.75f; 

    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Setup(ObstacleSpawner.ObstacleType type, Color color, bool passable, bool slideRight)
    {
        obstacleType = type;
        isPassable = passable;
        movesRight = slideRight;
        hasRewardedPoints = false; 

        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }

        if (obstacleType == ObstacleSpawner.ObstacleType.Car)
        {
            Vector3 scale = transform.localScale;
            float directionSign = movesRight ? 1f : -1f;
            transform.localScale = new Vector3(Mathf.Abs(scale.x) * directionSign, scale.y, scale.z);
        }
    }

    void Update()
    {
        float baseSpeed = GameManager.Instance != null ? GameManager.Instance.currentSpeed : 5f;
        float actualSpeed = baseSpeed * (obstacleType == ObstacleSpawner.ObstacleType.Car ? carSpeedMultiplier : 1f);

        if (obstacleType == ObstacleSpawner.ObstacleType.Car)
        {
            float direction = movesRight ? 1f : -1f;
            transform.Translate(Vector3.right * direction * actualSpeed * Time.deltaTime, Space.World);
        }
        else
        {
            transform.Translate(Vector3.left * actualSpeed * Time.deltaTime, Space.World);
        }

        float screenBoundX = Camera.main.orthographicSize * Camera.main.aspect + 5f;
        if (Mathf.Abs(transform.position.x - Camera.main.transform.position.x) > screenBoundX)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerController player = collision.GetComponent<PlayerController>();
            HandlePlayerCollision(player);
        }
    }

    private void HandlePlayerCollision(PlayerController player)
    {
        bool isUpgrade = obstacleType == ObstacleSpawner.ObstacleType.Heart ||
                         obstacleType == ObstacleSpawner.ObstacleType.ColorUpgrade ||
                         obstacleType == ObstacleSpawner.ObstacleType.FlashUpgrade ||
                         obstacleType == ObstacleSpawner.ObstacleType.SpawnUpgrade;

        if (isPassable) 
        {
            if (!hasRewardedPoints)
            {
                hasRewardedPoints = true;
                ObstacleSpawner spawner = FindFirstObjectByType<ObstacleSpawner>();

                if (obstacleType == ObstacleSpawner.ObstacleType.Heart)
                {
                    if (GameManager.Instance != null) GameManager.Instance.AddLife();
                }
                else if (obstacleType == ObstacleSpawner.ObstacleType.ColorUpgrade)
                {
                    if (spawner != null) spawner.RollbackColorDifficulty(0.2f);
                }
                else if (obstacleType == ObstacleSpawner.ObstacleType.FlashUpgrade)
                {
                    if (spawner != null) spawner.RollbackFlickerDifficulty(0.2f);
                }
                else if (obstacleType == ObstacleSpawner.ObstacleType.SpawnUpgrade)
                {
                    if (spawner != null) spawner.RollbackSpawnDifficulty(0.2f);
                }
                else
                {
                    if (GameManager.Instance != null) GameManager.Instance.AddScore(25);
                }
                
                if (isUpgrade)
                {
                    gameObject.SetActive(false);
                }
            }
        }
        else 
        {
            if (player != null)
            {
                if (player.PlayerHit())
                {
                    if (GameManager.Instance != null) GameManager.Instance.TakeDamage();
                }
            }
            else
            {
                if (GameManager.Instance != null) GameManager.Instance.TakeDamage();
            }
            
            gameObject.SetActive(false); 
        }
    }
}