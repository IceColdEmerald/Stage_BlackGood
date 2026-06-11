using UnityEngine;

public class RoadVisualizer : MonoBehaviour
{
    [SerializeField] float laneSpacing = 1.5f;
    [SerializeField] float roadLength = 50f;
    [SerializeField] Color asphaltColor = new Color(0.08f, 0.08f, 0.09f, 1f);
    [SerializeField] Color markingColor = new Color(1f, 0.85f, 0.45f, 0.95f);
    [SerializeField] float markingThickness = 0.06f;
    [SerializeField] float asphaltInset = 0.02f;
    [SerializeField] int laneMin = -3;
    [SerializeField] int laneMax = 3;

    void Start()
    {
        GenerateProceduralRoads();
    }

    void GenerateProceduralRoads()
    {
        for (int i = laneMin; i <= laneMax; i++)
        {
            float yPos = i * laneSpacing;

            GameObject asphalt = GameObject.CreatePrimitive(PrimitiveType.Quad);
            asphalt.name = $"Asphalt_Lane_{i}";
            asphalt.transform.SetParent(transform, false);
            asphalt.transform.localPosition = new Vector3(0f, yPos, 5f);
            asphalt.transform.localScale = new Vector3(roadLength * 2f, laneSpacing - asphaltInset * 2f, 1f);
            var mr = asphalt.GetComponent<MeshRenderer>();
            mr.material = new Material(Shader.Find("Sprites/Default"));
            mr.material.color = asphaltColor;

            if (i < laneMax)
            {
                float lineY = yPos + (laneSpacing / 2f);
                GameObject marking = GameObject.CreatePrimitive(PrimitiveType.Quad);
                marking.name = $"Divider_Mark_{i}";
                marking.transform.SetParent(transform, false);
                marking.transform.localPosition = new Vector3(0f, lineY, 4.9f);
                marking.transform.localScale = new Vector3(roadLength * 2f, markingThickness, 1f);
                var mmr = marking.GetComponent<MeshRenderer>();
                mmr.material = new Material(Shader.Find("Sprites/Default"));
                mmr.material.color = markingColor;
            }
        }
    }

    public float GetLaneSpacing() => laneSpacing;
    public float GetLaneHeight() => Mathf.Max(0.01f, laneSpacing - asphaltInset * 2f);
}
