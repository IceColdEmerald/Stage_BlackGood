using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] RectTransform hotbarRT;

    [Header("Movement Speed & Acceleration")]
    [SerializeField] float baseMoveSpeed = 3f;
    [SerializeField] float maxMoveSpeed = 7f;
    [SerializeField] float moveAcceleration = 0.02f;
    [SerializeField] float horizontalMargin = 0.5f;
    [SerializeField] float laneSpacing = 1.5f;
    [SerializeField] float laneSwitchSpeed = 12f;

    [Header("Invincibility Settings")]
    [SerializeField] float invincibilityDuration = 1.5f;
    [SerializeField] float flashInterval = 0.15f;

    int currentLane = 0;
    float targetY = 0f;
    float currentMoveSpeed;
    
    SpriteRenderer spriteRenderer;
    public bool IsInvincible { get; private set; } = false;

    void Start()
    {
        currentLane = 0;
        targetY = 0f;
        currentMoveSpeed = baseMoveSpeed;

        Vector3 p = transform.position;
        p.y = 0f;
        p.z = 0f;
        transform.position = p;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) spriteRenderer.sortingOrder = 100;
    }

    void Update()
    {
        HandlePlayerSpeedScaling();
        HandleHorizontalMovement();
        HandleLaneSwitching();
        SmoothMoveToLane();
    }

    void HandlePlayerSpeedScaling()
    {
        if (currentMoveSpeed < maxMoveSpeed)
        {
            currentMoveSpeed += moveAcceleration * Time.deltaTime;
            currentMoveSpeed = Mathf.Min(currentMoveSpeed, maxMoveSpeed);
        }
    }

    void HandleHorizontalMovement()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        Vector3 pos = transform.position;
        float halfWorldWidth = 0.5f * Camera.main.orthographicSize * Camera.main.aspect * 2f;
        float limit = Mathf.Max(0.1f, halfWorldWidth - horizontalMargin);
        
        pos.x += horizontalInput * currentMoveSpeed * Time.deltaTime;
        pos.x = Mathf.Clamp(pos.x, -limit, limit);
        transform.position = pos;
    }

    void HandleLaneSwitching()
    {
        int rangeLimit = GameManager.Instance != null ? GameManager.Instance.allowedLaneRange : 1;

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (currentLane < rangeLimit)
            {
                currentLane++;
                targetY = currentLane * laneSpacing;
            }
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (currentLane > -rangeLimit)
            {
                currentLane--;
                targetY = currentLane * laneSpacing;
            }
        }
    }

    void SmoothMoveToLane()
    {
        Vector3 pos = transform.position;
        pos.y = Mathf.Lerp(pos.y, targetY, laneSwitchSpeed * Time.deltaTime);
        transform.position = pos;
    }

    public bool PlayerHit()
    {
        if (IsInvincible || !gameObject.activeInHierarchy) return false;
        StartCoroutine(InvincibilityRoutine());
        return true;
    }

    IEnumerator InvincibilityRoutine()
    {
        IsInvincible = true;
        float timer = 0f;
        bool isVisible = true;

        while (timer < invincibilityDuration)
        {
            timer += Time.deltaTime;
            
            if (timer % (flashInterval * 2) < flashInterval)
            {
                isVisible = !isVisible;
                if (spriteRenderer != null) spriteRenderer.enabled = isVisible;
            }
            yield return null;
        }

        if (spriteRenderer != null) spriteRenderer.enabled = true;
        IsInvincible = false;
    }
}