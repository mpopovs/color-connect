using UnityEngine;
using System.Collections.Generic;

public class LevelGenerator : MonoBehaviour
{
    private enum ColorType
    {
        Red, Blue, Green, Yellow,
        Magenta, Cyan, Orange, Purple
    }

    private readonly ColorType[] availableColors =
    {
        ColorType.Red, ColorType.Blue, ColorType.Green, ColorType.Yellow,
        ColorType.Magenta, ColorType.Cyan, ColorType.Orange, ColorType.Purple
    };

    private const int MinGridSize = 4;
    private const int MaxGridSize = 8;
    private const int MinColorPairs = 2;
    private const int MaxColorPairs = 8;
    private const int MinDistance = 2;
    private const int MaxDistance = 6;

    private void Awake()
    {
        Random.InitState(System.Environment.TickCount);
    }

    public LevelData GenerateLevel(int levelNumber)
    {
        int gridSize = Mathf.Min(MinGridSize + (levelNumber / 5), MaxGridSize);
        int numColorPairs = Mathf.Min(MinColorPairs + (levelNumber / 3), MaxColorPairs);

        if (levelNumber == 11)
        {
            Debug.LogWarning($"Level 11 debug:");
            Debug.LogWarning($"- Grid size calculation: {MinGridSize} + ({levelNumber}/5) = {gridSize}");
            Debug.LogWarning($"- Color pairs calculation: {MinColorPairs} + ({levelNumber}/3) = {numColorPairs}");
            Debug.LogWarning($"- Available colors: {availableColors.Length}");
        }

        Debug.Log($"Level {levelNumber}: Grid size {gridSize}x{gridSize}, Color pairs: {numColorPairs}");
        return GenerateRandomLevel(gridSize, numColorPairs, levelNumber);
    }

    private LevelData GenerateRandomLevel(int gridSize, int numColorPairs, int levelNumber)
    {
        List<PointData> points = new List<PointData>();
        bool[,] occupiedPositions = new bool[gridSize, gridSize];

        List<ColorType> shuffledColors = new List<ColorType>(availableColors);
        Shuffle(shuffledColors);

        if (levelNumber == 11)
        {
            Debug.LogWarning("Level 11 - Shuffled colors: " + string.Join(", ", shuffledColors));
        }

        int pairsCreated = 0;
        for (int i = 0; i < numColorPairs; i++)
        {
            if (i >= shuffledColors.Count)
            {
                Debug.LogError($"Not enough colors at index {i}");
                break;
            }

            ColorType color = shuffledColors[i];

            if (levelNumber == 11)
            {
                Debug.LogWarning($"Level 11 - Creating pair {i+1} with color: {color}");
            }

            Vector2Int? firstPos = FindValidPosition(gridSize, occupiedPositions);
            if (!firstPos.HasValue)
            {
                Debug.LogError("Failed to place first point - no valid positions available");
                break;
            }

            occupiedPositions[firstPos.Value.x, firstPos.Value.y] = true;
            points.Add(new PointData { x = firstPos.Value.x, y = firstPos.Value.y, colorName = color.ToString() });

            if (levelNumber == 11)
            {
                Debug.LogWarning($"Level 11 - Added point 1 at ({firstPos.Value.x},{firstPos.Value.y}) with color {color}");
            }

            Vector2Int? secondPos = FindValidPairPosition(gridSize, occupiedPositions, firstPos.Value);
            if (!secondPos.HasValue)
            {
                Debug.LogError("Failed to place second point - no valid positions with path");
                occupiedPositions[firstPos.Value.x, firstPos.Value.y] = false;
                points.RemoveAt(points.Count - 1);
                continue;
            }

            // Ensure the second position is not the same as the first
            if (secondPos.Value.x == firstPos.Value.x && secondPos.Value.y == firstPos.Value.y)
            {
                Debug.LogError("Second point is at the same position as the first point");
                occupiedPositions[firstPos.Value.x, firstPos.Value.y] = false;
                points.RemoveAt(points.Count - 1);
                continue;
            }

            occupiedPositions[secondPos.Value.x, secondPos.Value.y] = true;
            points.Add(new PointData { x = secondPos.Value.x, y = secondPos.Value.y, colorName = color.ToString() });
            pairsCreated++;

            if (levelNumber == 11)
            {
                Debug.LogWarning($"Level 11 - Added point 2 at ({secondPos.Value.x},{secondPos.Value.y}) with color {color}");
                Debug.LogWarning($"Level 11 - Pair {pairsCreated} created successfully");
            }
        }

        if (levelNumber == 11)
        {
            Debug.LogWarning($"Level 11 - Created {pairsCreated} pairs out of {numColorPairs} requested");
            Debug.LogWarning($"Level 11 - Total points: {points.Count}");

            Dictionary<string, int> colorCounts = new Dictionary<string, int>();
            foreach (var point in points)
            {
                if (!colorCounts.ContainsKey(point.colorName))
                    colorCounts[point.colorName] = 0;
                colorCounts[point.colorName]++;
            }

            foreach (var pair in colorCounts)
            {
                Debug.LogWarning($"Level 11 - Color {pair.Key}: {pair.Value} points");
            }
        }

        Debug.Log($"Generated {points.Count} points total ({points.Count/2} pairs)");
        return new LevelData { gridSize = gridSize, points = points.ToArray() };
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    private Vector2Int? FindValidPosition(int gridSize, bool[,] occupied)
    {
        List<Vector2Int> availablePositions = new List<Vector2Int>();
        for (int x = 0; x < gridSize; x++)
            for (int y = 0; y < gridSize; y++)
                if (!occupied[x, y])
                    availablePositions.Add(new Vector2Int(x, y));

        if (availablePositions.Count == 0)
            return null;

        return availablePositions[Random.Range(0, availablePositions.Count)];
    }

    private Vector2Int? FindValidPairPosition(int gridSize, bool[,] occupied, Vector2Int firstPos)
    {
        List<Vector2Int> validPositions = new List<Vector2Int>();
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                if (!occupied[x, y] && IsValidPointPlacement(firstPos.x, firstPos.y, x, y, occupied))
                {
                    validPositions.Add(new Vector2Int(x, y));
                }
            }
        }

        if (validPositions.Count == 0)
            return null;

        return validPositions[Random.Range(0, validPositions.Count)];
    }

    private bool IsValidPointPlacement(int x1, int y1, int x2, int y2, bool[,] occupied)
    {
        if (x1 == x2 && y1 == y2)
            return false;

        int distance = Mathf.Abs(x2 - x1) + Mathf.Abs(y2 - y1);
        if (distance < MinDistance || distance > MaxDistance)
            return false;

        if (y1 == y2)
        {
            bool pathClear = true;
            int startX = Mathf.Min(x1, x2);
            int endX = Mathf.Max(x1, x2);

            for (int x = startX + 1; x < endX; x++)
            {
                if (occupied[x, y1])
                {
                    pathClear = false;
                    break;
                }
            }

            if (pathClear)
                return true;
        }

        if (x1 == x2)
        {
            bool pathClear = true;
            int startY = Mathf.Min(y1, y2);
            int endY = Mathf.Max(y1, y2);

            for (int y = startY + 1; y < endY; y++)
            {
                if (occupied[x1, y])
                {
                    pathClear = false;
                    break;
                }
            }

            if (pathClear)
                return true;
        }

        if (!occupied[x2, y1])
        {
            bool horizontalClear = true;
            int startX = Mathf.Min(x1, x2);
            int endX = Mathf.Max(x1, x2);

            for (int x = startX + 1; x < endX; x++)
            {
                if (occupied[x, y1])
                {
                    horizontalClear = false;
                    break;
                }
            }

            if (horizontalClear)
            {
                bool verticalClear = true;
                int startY = Mathf.Min(y1, y2);
                int endY = Mathf.Max(y1, y2);

                for (int y = startY + 1; y < endY; y++)
                {
                    if (occupied[x2, y])
                    {
                        verticalClear = false;
                        break;
                    }
                }

                if (verticalClear)
                    return true;
            }
        }

        if (!occupied[x1, y2])
        {
            bool verticalClear = true;
            int startY = Mathf.Min(y1, y2);
            int endY = Mathf.Max(y1, y2);

            for (int y = startY + 1; y < endY; y++)
            {
                if (occupied[x1, y])
                {
                    verticalClear = false;
                    break;
                }
            }

            if (verticalClear)
            {
                bool horizontalClear = true;
                int startX = Mathf.Min(x1, x2);
                int endX = Mathf.Max(x1, x2);

                for (int x = startX + 1; x < endX; x++)
                {
                    if (occupied[x, y2])
                    {
                        horizontalClear = false;
                        break;
                    }
                }

                if (horizontalClear)
                    return true;
            }
        }

        return false;
    }
}
