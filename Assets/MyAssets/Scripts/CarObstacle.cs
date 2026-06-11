using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class CarObstacle : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float extraCarSpeed = 6f; 

    private bool isPassable;
    private Vector3 moveDirection = Vector3.left;
    private SpriteRenderer spriteRenderer;
    private MeshRenderer meshRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        meshRenderer = GetComponent<MeshRenderer>();
    }

    void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.IsGameOver) return;

        float totalSpeed = GameManager.Instance.currentSpeed + extraCarSpeed;
        transform.Translate(moveDirection * totalSpeed * Time.deltaTime);

        float halfWidth = Camera.main.orthographicSize * Camera.main.aspect;
        float rightBound = Camera.main.transform.position.x + halfWidth + 4f;
        float leftBound = Camera.main.transform.position.x - halfWidth - 4f;

        if (moveDirection == Vector3.left && transform.position.x < leftBound)
        {
            gameObject.SetActive(false);
        }
        else if (moveDirection == Vector3.right && transform.position.x > rightBound)
        {
            gameObject.SetActive(false);
        }
    }

    public void Setup(Color assignedColor, bool passable, bool movingRight)
    {
        isPassable = passable;
        moveDirection = movingRight ? Vector3.right : Vector3.left;

        if (spriteRenderer != null) spriteRenderer.color = assignedColor;
        if (meshRenderer != null) meshRenderer.material.color = assignedColor;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (isPassable) return; 

            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null && !player.IsInvincible)
            {
                GameManager.Instance.TakeDamage();
                player.TriggerInvincibility();
                gameObject.SetActive(false);
            }
        }
    }
}