using UnityEngine;

public class ColorPoint : MonoBehaviour
{
    [SerializeField]
    private float pointSize = 2.0f; // Increased from 0.5f to 1.0f
    private Color pointColor = Color.white;
    public bool isConnected = false;
    public ColorPoint connectedTo;
    public int gridX, gridY;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("No SpriteRenderer found on ColorPoint object!");
            return;
        }
        transform.localScale = new Vector3(pointSize, pointSize, 1f);
        isConnected = false; // Ensure isConnected is reset on grid creation
        UpdateColor();
    }

    public void SetColor(Color color)
    {
        // Ensure full opacity
        color.a = 1f;
        pointColor = color;
        UpdateColor();
    }

    public Color GetColor() => pointColor;

    private void UpdateColor()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = pointColor;
            Debug.Log($"Updated sprite renderer color to: RGBA({spriteRenderer.color.r}, {spriteRenderer.color.g}, {spriteRenderer.color.b}, {spriteRenderer.color.a})");
        }
    }

    public void Connect(ColorPoint other)
    {
        isConnected = true;
        connectedTo = other;
        Debug.Log($"Connected point at ({gridX}, {gridY}) to point at ({other.gridX}, {other.gridY})");
    }

    public void Disconnect()
    {
        isConnected = false;
        connectedTo = null;
        Debug.Log($"Disconnected point at ({gridX}, {gridY})");
    }
}