using UnityEngine;

[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public class Obstacle : MonoBehaviour
{
    float screenLeftBoundary;
    bool isPassable;
    SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.IsGameOver) return;

        transform.Translate(Vector3.left * GameManager.Instance.currentSpeed * Time.deltaTime);

        float halfWidth = Camera.main.orthographicSize * Camera.main.aspect;
        screenLeftBoundary = Camera.main.transform.position.x - halfWidth - 2f;

        if (transform.position.x < screenLeftBoundary)
        {
            gameObject.SetActive(false);
        }
    }

    public void Setup(Color assignedColor, bool passable)
    {
        isPassable = passable;
        if (spriteRenderer != null) spriteRenderer.color = assignedColor;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (isPassable) return;

            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                if (!player.IsInvincible)
                {
                    GameManager.Instance.TakeDamage();
                    player.TriggerInvincibility();
                }

                gameObject.SetActive(false);
            }
        }
    }
}