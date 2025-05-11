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
    public System.Action onLevelComplete; // Add at the top
    [SerializeField] private float pointSize = 0.6f; // Adjust as needed
    [SerializeField] private LineRenderer borderLinePrefab; // Assign a LineRenderer prefab in Inspector
    private LineRenderer borderLineInstance;

    [SerializeField] private AudioClip drawClip;
    [SerializeField] private AudioClip nearPairClip;
    [SerializeField] private float minPitch = 0.8f;
    [SerializeField] private float maxPitch = 1.5f;
    [SerializeField] private float nearPairDistance = 1.0f; // How close to pair to trigger near sound

    [SerializeField] private ParticleSystem confettiPrefab;
    private ParticleSystem activeConfetti;

    private AudioSource currentLoopSource;

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

        if (activeConfetti != null)
        {
            Destroy(activeConfetti.gameObject);
        }
    }

    public void CreateGrid(int gridSize)
    {
        this.gridSize = gridSize;
        grid = new ColorPoint[gridSize, gridSize];
        AdjustCameraForGridSize();
        // DrawBorder();
    }

    private void AdjustCameraForGridSize()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        float gridWorldSize = gridSize * cellSize;
        float padding = 1.5f; // Adjust as needed

        // Get aspect ratio (width / height)
        float aspect = (float)Screen.width / Screen.height;

        // Calculate orthographic size needed to fit grid horizontally and vertically
        float verticalSize = (gridWorldSize / 2f) + padding;
        float horizontalSize = ((gridWorldSize / aspect) / 2f) + padding;

        // Use the larger size to ensure the whole grid fits
        mainCamera.orthographicSize = Mathf.Max(verticalSize, horizontalSize);

        // Center the camera on the grid
        mainCamera.transform.position = new Vector3(0, 0, mainCamera.transform.position.z);
    }

    public void SpawnPoint(int x, int y, Color color)
    {
        float offset = (gridSize - 1) * cellSize * 0.5f;
        Vector3 position = new Vector3(x * cellSize - offset, y * cellSize - offset, 0);
        GameObject pointObj = Instantiate(pointPrefab, position, Quaternion.identity, transform);

        // Set the point size
        pointObj.transform.localScale = new Vector3(pointSize, pointSize, 1f);

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
        if (touched != null)
        {
            // Check if any point of this color is already connected
            bool isColorConnected = false;
            Color touchedColor = touched.GetColor();
            foreach (ColorPoint point in grid)
            {
                if (point != null && point.GetColor() == touchedColor && point.isConnected)
                {
                    isColorConnected = true;
                    break;
                }
            }

            // If color is already connected, only allow clicking on connected points to remove the line
            if (isColorConnected)
            {
                if (touched.isConnected)
                {
                    // Remove the existing line
                    if (activeLines.ContainsKey(touchedColor))
                    {
                        Destroy(activeLines[touchedColor].gameObject);
                        activeLines.Remove(touchedColor);
                        DisconnectPointsByColor(touchedColor);
                    }
                }
                return; // Prevent starting new line if color is already connected
            }

            // Normal line drawing logic for unconnected colors
            selectedPoint = touched;
            currentLinePoints.Clear();
            currentLinePoints.Add(touched.transform.position);

            // Create temporary line
            Color startColor = touched.GetColor();
            if (activeLines.ContainsKey(startColor))
            {
                var existingLine = activeLines[startColor];
                var clickHandler = existingLine.GetComponent<LineClickHandler>();
                if (clickHandler != null)
                    clickHandler.DisableClicking();
                Destroy(existingLine.gameObject);
                activeLines.Remove(startColor);
                DisconnectPointsByColor(startColor);
            }

            LineRenderer tempLine = Instantiate(linePrefab, transform);
            tempLine.startColor = tempLine.endColor = startColor;
            activeLines[startColor] = tempLine;
        }
    }

    private void DisconnectPointsByColor(Color color)
    {
        foreach (ColorPoint point in grid)
        {
            if (point != null && point.GetColor() == color)
            {
                point.isConnected = false;
                point.connectedTo = null;
            }
        }
        
        // Remove any drawn lines with this color
        drawnLines = drawnLines.Where(l => 
            !IsLineOfColor(l.Item1, l.Item2, color)).ToList();
    }
    
    private bool IsLineOfColor(Vector2 start, Vector2 end, Color color)
    {
        foreach (ColorPoint point in grid)
        {
            if (point != null && point.GetColor() == color)
            {
                Vector2 pointPos = point.transform.position;
                if (pointPos == start || pointPos == end)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private void HandleInputMove(Vector2 position)
    {
        if (selectedPoint == null) return;

        Vector3 worldPos3D = mainCamera.ScreenToWorldPoint(new Vector3(position.x, position.y, -mainCamera.transform.position.z));
        Vector2 worldPos = new Vector2(worldPos3D.x, worldPos3D.y);

        ColorPoint hoveredPoint = GetPointAtPosition(position);
        if (hoveredPoint != null && hoveredPoint != selectedPoint)
        {
            if (hoveredPoint.GetColor() == selectedPoint.GetColor() && !hoveredPoint.isConnected)
            {
                if (currentLinePoints.Count > 0)
                {
                    currentLinePoints[currentLinePoints.Count - 1] = (Vector2)hoveredPoint.transform.position;
                    LineRenderer line = activeLines[selectedPoint.GetColor()];
                    line.positionCount = currentLinePoints.Count;
                    for (int i = 0; i < currentLinePoints.Count; i++)
                        line.SetPosition(i, currentLinePoints[i]);
                }
                return;
            }
        }

        if (currentLinePoints.Count == 0 || Vector2.Distance(worldPos, currentLinePoints[currentLinePoints.Count - 1]) > MIN_POINT_DISTANCE)
        {
            currentLinePoints.Add(worldPos);

            LineRenderer line = activeLines[selectedPoint.GetColor()];
            line.positionCount = currentLinePoints.Count;
            for (int i = 0; i < currentLinePoints.Count; i++)
            {
                Vector3 pointPos = new Vector3(currentLinePoints[i].x, currentLinePoints[i].y, 0);
                line.SetPosition(i, pointPos);
            }
        }

        if (selectedPoint != null && activeLines.ContainsKey(selectedPoint.GetColor()))
        {
            float pitch = minPitch;

            if (hoveredPoint != null && hoveredPoint != selectedPoint &&
                hoveredPoint.GetColor() == selectedPoint.GetColor() && !hoveredPoint.isConnected)
            {
                PlayLineAudio(nearPairClip, maxPitch);
            }
            else
            {
                float minDist = float.MaxValue;
                foreach (ColorPoint point in grid)
                {
                    if (point != null && point != selectedPoint && point.GetColor() == selectedPoint.GetColor() && !point.isConnected)
                    {
                        float dist = Vector2.Distance(point.transform.position, currentLinePoints.Last());
                        if (dist < minDist) minDist = dist;
                    }
                }
                pitch = Mathf.Lerp(maxPitch, minPitch, Mathf.Clamp01(minDist / nearPairDistance));
                PlayLineAudio(drawClip, pitch);
            }
        }
    }

    private void HandleInputEnd()
    {
        if (selectedPoint == null) return;

        ColorPoint hoveredPoint = GetPointAtPosition(Input.mousePosition);
        if (hoveredPoint != null && hoveredPoint != selectedPoint)
        {
            if (hoveredPoint.GetColor() == selectedPoint.GetColor() && !hoveredPoint.isConnected)
            {
                if (currentLinePoints.Count > 0 && currentLinePoints[currentLinePoints.Count - 1] != (Vector2)hoveredPoint.transform.position)
                {
                    currentLinePoints.Add((Vector2)hoveredPoint.transform.position);
                }

                if (!DoesCurveIntersectAnyLine(currentLinePoints))
                {
                    FinalizeLine(selectedPoint, hoveredPoint, currentLinePoints);
                }
                else
                {
                    Color startColor = selectedPoint.GetColor();
                    if (activeLines.ContainsKey(startColor))
                    {
                        Destroy(activeLines[startColor].gameObject);
                        activeLines.Remove(startColor);
                    }
                }
            }
            else
            {
                Color startColor = selectedPoint.GetColor();
                if (activeLines.ContainsKey(startColor))
                {
                    Destroy(activeLines[startColor].gameObject);
                    activeLines.Remove(startColor);
                }
            }
        }
        else
        {
            if (selectedPoint != null)
            {
                Color startColor = selectedPoint.GetColor();
                if (activeLines.ContainsKey(startColor))
                {
                    Destroy(activeLines[startColor].gameObject);
                    activeLines.Remove(startColor);
                }
            }
        }

        StopLineAudio();

        currentLinePoints.Clear();
        selectedPoint = null;
    }

    private ColorPoint GetPointAtPosition(Vector2 screenPosition)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
        
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

    private bool LinesIntersect(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
    {
        if (a1 == b1 || a1 == b2 || a2 == b1 || a2 == b2) return false;

        float d = (a2.x - a1.x) * (b2.y - b1.y) - (a2.y - a1.y) * (b2.x - b1.x);
        if (d == 0) return false;

        float u = ((b1.x - a1.x) * (b2.y - b1.y) - (b1.y - a1.y) * (b2.x - b1.x)) / d;
        float v = ((b1.x - a1.x) * (a2.y - a1.y) - (b1.y - a1.y) * (a2.x - a1.x)) / d;

        return (u > 0 && u < 1 && v > 0 && v < 1);
    }

    private bool DoesCurveIntersectAnyLine(List<Vector2> curvePoints)
    {
        if (curvePoints.Count < 2) return false;

        foreach (var existingLine in drawnLines)
        {
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
        if (points.Count < 2) return;
        
        Color startColor = start.GetColor();
        LineRenderer line = activeLines[startColor];
        
        line.positionCount = points.Count;
        for (int i = 0; i < points.Count; i++)
        {
            line.SetPosition(i, points[i]);
        }
        
        for (int i = 0; i < points.Count - 1; i++)
        {
            drawnLines.Add((points[i], points[i + 1]));
        }

        EdgeCollider2D edgeCollider = line.gameObject.AddComponent<EdgeCollider2D>();
        Vector2[] colliderPoints = new Vector2[points.Count];
        for (int i = 0; i < points.Count; i++)
            colliderPoints[i] = points[i];
        edgeCollider.points = colliderPoints;
        edgeCollider.edgeRadius = 0.2f;
        edgeCollider.isTrigger = false;

        var clickHandler = line.gameObject.AddComponent<LineClickHandler>();
        clickHandler.lineColor = startColor;
        clickHandler.onLineClicked = HandleLineClicked;
        clickHandler.EnableClicking();

        start.Connect(end);
        end.Connect(start);
        
        Debug.Log($"Connected {start.gridX},{start.gridY} to {end.gridX},{end.gridY}");

        CheckForLevelComplete();
    }

    private void HandleLineClicked(LineRenderer line)
    {
        Color color = line.startColor;
        if (activeLines.ContainsKey(color))
        {
            Destroy(activeLines[color].gameObject);
            activeLines.Remove(color);
            DisconnectPointsByColor(color);
        }
    }

    private void CheckForLevelComplete()
    {
        foreach (ColorPoint point in grid)
        {
            if (point != null && !point.isConnected && point.GetColor() != Color.white)
                return;
        }
        Debug.Log("Level Complete!");
        PlayConfettiEffect();
        onLevelComplete?.Invoke();
    }

    private void PlayConfettiEffect()
    {
        if (confettiPrefab != null)
        {
            // Position the confetti above the center of the grid
            float offset = (gridSize - 1) * cellSize * 0.5f;
            Vector3 position = new Vector3(0, offset, -1); // Slightly in front of the grid
            
            if (activeConfetti != null)
            {
                Destroy(activeConfetti.gameObject);
            }
            
            activeConfetti = Instantiate(confettiPrefab, position, Quaternion.Euler(-90, 0, 0));
            // Auto-destroy after a few seconds
            Destroy(activeConfetti.gameObject, 3f);
        }
    }

    private void PlayLineAudio(AudioClip clip, float pitch = 1f)
    {
        if (currentLoopSource != null)
        {
            currentLoopSource.Stop();
            Destroy(currentLoopSource);
        }
        currentLoopSource = gameObject.AddComponent<AudioSource>();
        currentLoopSource.clip = clip;
        currentLoopSource.loop = true;
        currentLoopSource.pitch = pitch;
        currentLoopSource.Play();
    }

    private void StopLineAudio()
    {
        if (currentLoopSource != null)
        {
            currentLoopSource.Stop();
            Destroy(currentLoopSource);
            currentLoopSource = null;
        }
    }
}