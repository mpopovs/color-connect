using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[RequireComponent(typeof(LineRenderer), typeof(EdgeCollider2D))]
public class LineClickHandler : MonoBehaviour
{
    public UnityEvent<LineRenderer> onLineClickedUnityEvent = new UnityEvent<LineRenderer>();
    public System.Action<LineRenderer> onLineClicked;
    private LineRenderer lineRenderer;
    public Color lineColor;
    private bool canClick = true;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        // Make sure this GameObject can be clicked
        gameObject.layer = LayerMask.NameToLayer("Default");
    }

    private void OnMouseUpAsButton()
    {
        if (!canClick) return;

        Debug.Log("Line clicked!");
        onLineClicked?.Invoke(lineRenderer);
        onLineClickedUnityEvent?.Invoke(lineRenderer);
    }

    public void EnableClicking()
    {
        canClick = true;
    }

    public void DisableClicking()
    {
        canClick = false;
    }
}