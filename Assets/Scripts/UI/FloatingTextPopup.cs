using UnityEngine;

public class FloatingTextPopup : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.9f;
    [SerializeField] private float riseSpeed = 0.6f;

    private TextMesh textMesh;
    private float elapsed;
    private Color initialColor;

    public static void Spawn(string message, Color color, Vector3 position)
    {
        GameObject popupObject = new GameObject("FloatingTextPopup");
        popupObject.transform.position = position;
        FloatingTextPopup popup = popupObject.AddComponent<FloatingTextPopup>();
        popup.Initialize(message, color);
    }

    private void Initialize(string message, Color color)
    {
        textMesh = gameObject.AddComponent<TextMesh>();
        textMesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = 0.08f;
        textMesh.fontSize = 32;
        textMesh.text = message;
        textMesh.color = color;
        initialColor = color;

        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sortingLayerName = "UI_World";
            renderer.sortingOrder = 20;
        }
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        transform.position += Vector3.up * riseSpeed * Time.deltaTime;

        if (textMesh != null)
        {
            float alpha = 1f - Mathf.Clamp01(elapsed / lifetime);
            textMesh.color = new Color(initialColor.r, initialColor.g, initialColor.b, alpha);
        }

        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}

