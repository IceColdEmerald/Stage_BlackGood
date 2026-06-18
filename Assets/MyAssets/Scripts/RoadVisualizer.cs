using System.Collections.Generic;
using UnityEngine;

public class RoadVisualizer : MonoBehaviour
{
    [Header("Road Setup")]
    [SerializeField] float laneSpacing = 1.5f;
    [SerializeField] float roadLength = 50f;
    [SerializeField] Color asphaltColor = new Color(0.35f, 0.35f, 0.35f, 1f);
    [SerializeField] float asphaltInset = 0.02f;
    [SerializeField] int laneMin = -3;
    [SerializeField] int laneMax = 3;

    [Header("Dashed Markings")]
    [SerializeField] Color markingColor = new Color(0.9f, 0.85f, 0.3f, 1f);
    [SerializeField] float markingThickness = 0.08f;
    [SerializeField] float dashLength = 0.6f;
    [SerializeField] float dashGap = 0.4f;

    float leftBound;
    float rightBound;
    float totalPatternLength;
    List<Transform> activeDashes = new List<Transform>();

    void Start()
    {
        leftBound = -roadLength;
        rightBound = roadLength;
        totalPatternLength = dashLength + dashGap;

        GenerateProceduralRoads();
    }

    void Update()
    {
        ScrollMarkings();
    }

    void GenerateProceduralRoads()
    {
        Material spriteMat = new Material(Shader.Find("Sprites/Default"));

        for (int i = laneMin; i <= laneMax; i++)
        {
            float yPos = i * laneSpacing;

            GameObject asphalt = GameObject.CreatePrimitive(PrimitiveType.Quad);
            asphalt.name = $"Asphalt_Lane_{i}";
            asphalt.transform.SetParent(transform, false);
            asphalt.transform.localPosition = new Vector3(0f, yPos, 5f);
            asphalt.transform.localScale = new Vector3(roadLength * 2f, laneSpacing - asphaltInset * 2f, 1f);

            Destroy(asphalt.GetComponent<Collider>());
            
            var mr = asphalt.GetComponent<MeshRenderer>();
            mr.material = spriteMat;
            mr.material.color = asphaltColor;

            if (i < laneMax)
            {
                float lineY = yPos + (laneSpacing / 2f);
                
                for (float x = leftBound; x <= rightBound; x += totalPatternLength)
                {
                    GameObject dash = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    dash.name = $"Dash_{i}";
                    dash.transform.SetParent(transform, false);
                    dash.transform.localPosition = new Vector3(x, lineY, 4.9f);
                    dash.transform.localScale = new Vector3(dashLength, markingThickness, 1f);
                    
                    Destroy(dash.GetComponent<Collider>());

                    var dmr = dash.GetComponent<MeshRenderer>();
                    dmr.material = spriteMat;
                    dmr.material.color = markingColor;

                    activeDashes.Add(dash.transform);
                }
            }
        }
    }

    void ScrollMarkings()
    {
        float currentSpeed = GameManager.Instance != null ? GameManager.Instance.currentSpeed : 5f;
        float movement = currentSpeed * Time.deltaTime;

        for (int i = 0; i < activeDashes.Count; i++)
        {
            Transform dash = activeDashes[i];
            Vector3 pos = dash.localPosition;
            pos.x -= movement;

            if (pos.x < leftBound)
            {
                pos.x += (rightBound - leftBound) + dashGap;
            }

            dash.localPosition = pos;
        }
    }

    public float GetLaneSpacing() => laneSpacing;
    public float GetLaneHeight() => Mathf.Max(0.01f, laneSpacing - asphaltInset * 2f);
}