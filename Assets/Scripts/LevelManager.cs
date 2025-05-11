using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro; // Add TextMeshPro namespace

[Serializable]
public class LevelData
{
    public int gridSize;
    public PointData[] points;
}

[Serializable]
public class PointData
{
    public int x;
    public int y;
    public string colorName;
}

public class LevelManager : MonoBehaviour
{
    [SerializeField]
    private GameBoard gameBoard;
    [SerializeField]
    private Button nextLevelButton;
    [SerializeField]
    private TextMeshProUGUI levelText;

    private LevelGenerator levelGenerator;
    private int currentLevel = 0;

    private void Start()
    {
        levelGenerator = GetComponent<LevelGenerator>();
        if (levelGenerator == null)
        {
            levelGenerator = gameObject.AddComponent<LevelGenerator>();
        }

        if (PlayerPrefs.HasKey("SavedLevel"))
            currentLevel = PlayerPrefs.GetInt("SavedLevel");
        else
            currentLevel = 0;

        if (nextLevelButton != null)
        {
            nextLevelButton.gameObject.SetActive(false);
            nextLevelButton.onClick.AddListener(OnNextLevelClick);
        }

        if (gameBoard != null)
            gameBoard.onLevelComplete += OnLevelComplete;

        LoadLevel(currentLevel);
        UpdateLevelText();
    }

    private void OnNextLevelClick()
    {
        Debug.Log($"Next level button clicked. Current level: {currentLevel}");
        currentLevel++; // Increment level before loading
        LoadLevel(currentLevel);
        nextLevelButton.gameObject.SetActive(false);
        UpdateLevelText();
    }

    private void OnLevelComplete()
    {
        Debug.Log("Level completed! Showing next level button.");
        if (nextLevelButton != null)
            nextLevelButton.gameObject.SetActive(true);
    }

    private void UpdateLevelText()
    {
        if (levelText != null)
        {
            levelText.text = $"Level {currentLevel + 1}"; // +1 for display since we start at 0
        }
    }

    public void LoadLevel(int levelNumber)
    {
        Debug.Log($"Loading level {levelNumber}");
        LevelData levelData = levelGenerator.GenerateLevel(levelNumber);
        SetupLevel(levelData);
    }

    private void SetupLevel(LevelData levelData)
    {
        gameBoard.ClearBoard();
        gameBoard.CreateGrid(levelData.gridSize);

        foreach (PointData point in levelData.points)
        {
            Color color = GetColorFromName(point.colorName);
            gameBoard.SpawnPoint(point.x, point.y, color);
        }
    }

    private Color GetColorFromName(string colorName)
    {
        Color color;
        switch (colorName.ToLower())
        {
            case "red": color = Color.red; break;
            case "blue": color = Color.blue; break;
            case "green": color = Color.green; break;
            case "yellow": color = Color.yellow; break;
            case "magenta": color = Color.magenta; break;
            case "cyan": color = Color.cyan; break;
            case "orange": color = new Color(1f, 0.5f, 0f, 1f); break;
            case "purple": color = new Color(0.5f, 0f, 0.5f, 1f); break;
            default: color = Color.white; break;
        }
        return color;
    }

    private void OnApplicationQuit()
    {
        PlayerPrefs.SetInt("SavedLevel", currentLevel);
        PlayerPrefs.Save();
    }
}
