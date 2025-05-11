using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class GameBoard : MonoBehaviour
{
    public GameObject pointPrefab;
    public int gridSize = 4;
    public float cellSize = 1f;
    public LineRenderer linePrefab;
    public Button nextLevelButton;

    private ColorPoint[,] grid;
    private Dictionary<Color, LineRenderer> activeLines = new Dictionary<Color, LineRenderer>();
    private ColorPoint selectedPoint;
    private Camera mainCamera;
    private List<(Vector2, Vector2)> drawnLines = new List<(Vector2, Vector2)>();
    private List<Vector2> currentLinePoints = new List<Vector2>();
    private const float MIN_POINT_DISTANCE = 0.1f; // Minimum distance between curve points

    private void Start()
    {
        mainCamera = Camera.main;
        if (nextLevelButton != null)
            nextLevelButton.gameObject.SetActive(false);
    }

    public void ClearBoard()
    {
        if (grid != null)
        {
            foreach (ColorPoint point in grid)
            {
                if (point != null)
                    Destroy(point.gameObject);
            }
        }
        grid = null;
        foreach (var line in activeLines.Values)
        {
            if (line != null)
                Destroy(line.gameObject);
        }
        activeLines.Clear();
        drawnLines.Clear();
    }

    public void CreateGrid(int gridSize)
    {
        this.gridSize = gridSize;
        grid = new ColorPoint[gridSize, gridSize];
    }

    public void SpawnPoint(int x, int y, Color color)
    {
        float offset = (gridSize - 1) * cellSize * 0.5f;
        Vector3 position = new Vector3(x * cellSize - offset, y * cellSize - offset, 0);
        GameObject pointObj = Instantiate(pointPrefab, position, Quaternion.identity, transform);
        ColorPoint point = pointObj.GetComponent<ColorPoint>();
        point.gridX = x;
        point.gridY = y;
        point.SetColor(color);
        point.isConnected = false;
        point.connectedTo = null;
        grid[x, y] = point;
    }

    private void Update()
    {
        // Handle touch input for mobile
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Vector2 touchPosition = touch.position;
            
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    HandleInputStart(touchPosition);
                    break;
                    
                case TouchPhase.Moved:
                    HandleInputMove(touchPosition);
                    break;
                    
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    HandleInputEnd();
                    break;
            }
        }
        // Handle mouse input for desktop/simulator
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                Debug.Log($"Mouse clicked at position: {Input.mousePosition}");
                HandleInputStart(Input.mousePosition);
            }
            else if (Input.GetMouseButton(0))
            {
                Debug.Log($"Mouse dragging at position: {Input.mousePosition}");
                HandleInputMove(Input.mousePosition);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                Debug.Log("Mouse released");
                HandleInputEnd();
            }
        }
    }

    private void HandleInputStart(Vector2 position)
    {
        ColorPoint touched = GetPointAtPosition(position);
        if (touched != null && !touched.isConnected)
        {
            selectedPoint = touched;
            currentLinePoints.Clear();
            currentLinePoints.Add(touched.transform.position);
            
            // Create temporary line
            Color startColor = touched.GetColor();
            LineRenderer tempLine = Instantiate(linePrefab, transform);
            tempLine.startColor = tempLine.endColor = startColor;
            if (activeLines.ContainsKey(startColor))
            {
                Destroy(activeLines[startColor].gameObject);
                activeLines.Remove(startColor);
            }
            activeLines[startColor] = tempLine;
        }
    }

    private void HandleInputMove(Vector2 position)
    {
        if (selectedPoint == null) return;

        // Convert screen position to world position and ensure it's in 2D space
        Vector3 worldPos3D = mainCamera.ScreenToWorldPoint(new Vector3(position.x, position.y, -mainCamera.transform.position.z));
        Vector2 worldPos = new Vector2(worldPos3D.x, worldPos3D.y);
        
        // Only add point if it's far enough from the last point
        if (currentLinePoints.Count == 0 || Vector2.Distance(worldPos, currentLinePoints[currentLinePoints.Count - 1]) > MIN_POINT_DISTANCE)
        {
            currentLinePoints.Add(worldPos);
            
            // Update line renderer with proper z-position
            LineRenderer line = activeLines[selectedPoint.GetColor()];
            line.positionCount = currentLinePoints.Count;
            for (int i = 0; i < currentLinePoints.Count; i++)
            {
                Vector3 pointPos = new Vector3(currentLinePoints[i].x, currentLinePoints[i].y, 0);
                line.SetPosition(i, pointPos);
            }
        }

        // Check if we're hovering over the matching endpoint
        ColorPoint hoveredPoint = GetPointAtPosition(position);
        if (hoveredPoint != null && hoveredPoint != selectedPoint)
        {
            if (hoveredPoint.GetColor() == selectedPoint.GetColor() && !hoveredPoint.isConnected)
            {
                // Snap the last point to the target point
                if (currentLinePoints.Count > 0)
                {
                    currentLinePoints[currentLinePoints.Count - 1] = (Vector2)hoveredPoint.transform.position;
                    LineRenderer line = activeLines[selectedPoint.GetColor()];
                    line.SetPosition(line.positionCount - 1, hoveredPoint.transform.position);
                }
            }
        }
    }

    private void HandleInputEnd()
    {
        if (selectedPoint == null) return;

        // Convert screen position to world position and ensure it's in 2D space
        Vector3 mousePos3D = mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -mainCamera.transform.position.z));
        Vector2 mousePos = new Vector2(mousePos3D.x, mousePos3D.y);
        
        ColorPoint hoveredPoint = GetPointAtPosition(Input.mousePosition);
        if (hoveredPoint != null && hoveredPoint != selectedPoint)
        {
            if (hoveredPoint.GetColor() == selectedPoint.GetColor() && !hoveredPoint.isConnected)
            {
                // Add final point to the curve if needed
                if (currentLinePoints.Count > 0 && currentLinePoints[currentLinePoints.Count - 1] != (Vector2)hoveredPoint.transform.position)
                {
                    currentLinePoints.Add((Vector2)hoveredPoint.transform.position);
                }

                // Check if the curved line intersects with any other lines
                if (!DoesCurveIntersectAnyLine(currentLinePoints))
                {
                    FinalizeLine(selectedPoint, hoveredPoint, currentLinePoints);
                }
                else
                {
                    // Remove the temporary line if there's an intersection
                    Color startColor = selectedPoint.GetColor();
                    if (activeLines.ContainsKey(startColor))
                    {
                        Destroy(activeLines[startColor].gameObject);
                        activeLines.Remove(startColor);
                    }
                }
            }
        }

        currentLinePoints.Clear();
        selectedPoint = null;
    }

    private ColorPoint GetPointAtPosition(Vector2 screenPosition)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        Vector2 rayOrigin = ray.origin;
        Vector2 rayDirection = ray.direction;
        Debug.Log($"Raycast from: {rayOrigin}, direction: {rayDirection}");

        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, rayDirection);
        Debug.Log($"Raycast hit something: {hit.collider != null}, Hit point: {(hit.collider != null ? hit.point.ToString() : "none")}");
        
        if (hit.collider != null)
        {
            ColorPoint point = hit.collider.GetComponent<ColorPoint>();
            if (point != null)
            {
                Debug.Log($"Found point at grid position ({point.gridX}, {point.gridY}) with color: {point.GetColor()}");
            }
            return point;
        }
        return null;
    }

    public ColorPoint GetPointAt(int x, int y)
    {
        if (x >= 0 && x < gridSize && y >= 0 && y < gridSize)
        {
            return grid[x, y];
        }
        return null;
    }

    private void DrawLine(ColorPoint start, ColorPoint end)
    {
        Vector2 startPos = start.transform.position;
        Vector2 endPos = end.transform.position;

        // Prevent crossing lines
        foreach (var existingLine in drawnLines)
        {
            if (LinesIntersect(existingLine.Item1, existingLine.Item2, startPos, endPos))
            {
                Debug.Log("Line would cross another line. Not allowed.");
                return;
            }
        }

        Color startColor = start.GetColor();
        if (activeLines.ContainsKey(startColor))
        {
            Destroy(activeLines[startColor].gameObject);
            activeLines.Remove(startColor);

            // Remove previous line for this color
            drawnLines = drawnLines.Where(l => !IsSameLine(l.Item1, l.Item2, startPos, endPos)).ToList();
        }

        LineRenderer newLine = Instantiate(linePrefab, transform);
        newLine.positionCount = 2;
        newLine.SetPosition(0, startPos);
        newLine.SetPosition(1, endPos);
        newLine.startColor = newLine.endColor = startColor;

        activeLines[startColor] = newLine;
        drawnLines.Add((startPos, endPos));

        start.Connect(end);
        end.Connect(start);

        CheckForLevelComplete();
    }

    private bool IsSameLine(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
    {
        return (a1 == b1 && a2 == b2) || (a1 == b2 && a2 == b1);
    }

    private bool LinesIntersect(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
    {
        // Exclude lines that share a point
        if (a1 == b1 || a1 == b2 || a2 == b1 || a2 == b2) return false;

        float d = (a2.x - a1.x) * (b2.y - b1.y) - (a2.y - a1.y) * (b2.x - b1.x);
        if (d == 0) return false; // Parallel

        float u = ((b1.x - a1.x) * (b2.y - b1.y) - (b1.y - a1.y) * (b2.x - b1.x)) / d;
        float v = ((b1.x - a1.x) * (a2.y - a1.y) - (b1.y - a1.y) * (a2.x - a1.x)) / d;

        return (u > 0 && u < 1 && v > 0 && v < 1);
    }

    private bool DoesCurveIntersectAnyLine(List<Vector2> curvePoints)
    {
        if (curvePoints.Count < 2) return false;

        foreach (var existingLine in drawnLines)
        {
            // Check each segment of the curve against the existing line
            for (int i = 0; i < curvePoints.Count - 1; i++)
            {
                if (LinesIntersect(existingLine.Item1, existingLine.Item2, curvePoints[i], curvePoints[i + 1]))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private void FinalizeLine(ColorPoint start, ColorPoint end, List<Vector2> points)
    {
        Color startColor = start.GetColor();
        LineRenderer line = activeLines[startColor];
        
        // Store the curve segments for intersection checking
        for (int i = 0; i < points.Count - 1; i++)
        {
            drawnLines.Add((points[i], points[i + 1]));
        }

        start.Connect(end);
        end.Connect(start);

        CheckForLevelComplete();
    }

    private void CheckForLevelComplete()
    {
        foreach (ColorPoint point in grid)
        {
            if (point != null && !point.isConnected && point.GetColor() != Color.white)
            {
                return;
            }
        }

        Debug.Log("Level Complete!");
        if (nextLevelButton != null)
        {
            nextLevelButton.gameObject.SetActive(true);
        }
    }
}