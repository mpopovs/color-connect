using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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

        // Make sure we don't try to create more color pairs than available colors
        numColorPairs = Mathf.Min(numColorPairs, availableColors.Length);

        Debug.Log($"Level {levelNumber}: Grid size {gridSize}x{gridSize}, Color pairs: {numColorPairs}");
        return GenerateRandomLevel(gridSize, numColorPairs);
    }

    private LevelData GenerateRandomLevel(int gridSize, int numColorPairs)
    {
        List<PointData> points = new List<PointData>();
        bool[,] occupied = new bool[gridSize, gridSize];

        // Shuffle available colors and pick the first numColorPairs
        List<ColorType> colorPool = availableColors.OrderBy(x => Random.value).ToList();
        colorPool = colorPool.Take(numColorPairs).ToList();

        foreach (var color in colorPool)
        {
            // First point
            int x1, y1;
            do
            {
                x1 = Random.Range(0, gridSize);
                y1 = Random.Range(0, gridSize);
            } while (occupied[x1, y1]);
            occupied[x1, y1] = true;
            points.Add(new PointData { x = x1, y = y1, colorName = color.ToString() });

            // Second point
            int x2, y2;
            do
            {
                x2 = Random.Range(0, gridSize);
                y2 = Random.Range(0, gridSize);
            } while (occupied[x2, y2] || (x1 == x2 && y1 == y2));
            occupied[x2, y2] = true;
            points.Add(new PointData { x = x2, y = y2, colorName = color.ToString() });
        }

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