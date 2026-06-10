using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class AdaptiveLine : MonoBehaviour
{
    public float baseWorldWidth = 0.05f;
    public float baseOrthoSize = 0f;

    private LineRenderer lr;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        if (lr == null) enabled = false;
    }

    void Start()
    {
        if (baseOrthoSize <= 0f && GameManager.Instance != null)
            baseOrthoSize = GameManager.Instance.GetBaseOrthoSize();
        if (baseOrthoSize <= 0f)
            baseOrthoSize = 2.8f;
    }

    void Update()
    {
        if (Camera.main == null || lr == null) return;
        float scale = baseOrthoSize / Mathf.Max(0.0001f, Camera.main.orthographicSize);
        lr.widthMultiplier = baseWorldWidth * scale;
    }
}
