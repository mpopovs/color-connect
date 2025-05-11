using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ButtonManager : MonoBehaviour
{
    [SerializeField] private Button exitButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private TextMeshProUGUI exitButtonText;
    [SerializeField] private TextMeshProUGUI resetButtonText;

    private float doubleClickTime = 0.5f; // Time window for double click
    private float lastExitClickTime;
    private float lastResetClickTime;
    private bool isExitPressed;
    private bool isResetPressed;

    private void Start()
    {
        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitButtonClick);
        
        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetButtonClick);

        ResetButtonStates();
    }

    private void ResetButtonStates()
    {
        isExitPressed = false;
        isResetPressed = false;
        if (exitButtonText != null)
            exitButtonText.text = "Exit Game";
        if (resetButtonText != null)
            resetButtonText.text = "Reset Progress";
    }

    private void OnExitButtonClick()
    {
        if (!isExitPressed)
        {
            // First click
            isExitPressed = true;
            lastExitClickTime = Time.time;
            if (exitButtonText != null)
                exitButtonText.text = "Click again to Exit";
        }
        else
        {
            // Second click - check if within time window
            if (Time.time - lastExitClickTime <= doubleClickTime)
            {
                // Exit the game
                #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
                #else
                    Application.Quit();
                #endif
            }
            else
            {
                // Reset if too much time has passed
                isExitPressed = false;
                lastExitClickTime = Time.time;
                if (exitButtonText != null)
                    exitButtonText.text = "Exit Game";
            }
        }
    }

    private void OnResetButtonClick()
    {
        if (!isResetPressed)
        {
            // First click
            isResetPressed = true;
            lastResetClickTime = Time.time;
            if (resetButtonText != null)
                resetButtonText.text = "Click again to Reset";
        }
        else
        {
            // Second click - check if within time window
            if (Time.time - lastResetClickTime <= doubleClickTime)
            {
                // Reset saved progress
                PlayerPrefs.DeleteKey("SavedLevel");
                PlayerPrefs.Save();
                
                // Reload the first level
                LevelManager levelManager = FindObjectOfType<LevelManager>();
                if (levelManager != null)
                {
                    levelManager.LoadLevel(0);
                }
                
                if (resetButtonText != null)
                    resetButtonText.text = "Progress Reset!";
                
                // Reset button state after a short delay
                Invoke("ResetButtonStates", 1.5f);
            }
            else
            {
                // Reset if too much time has passed
                isResetPressed = false;
                lastResetClickTime = Time.time;
                if (resetButtonText != null)
                    resetButtonText.text = "Reset Progress";
            }
        }
    }

    private void Update()
    {
        // Reset first click state if too much time has passed
        if (isExitPressed && Time.time - lastExitClickTime > doubleClickTime)
        {
            isExitPressed = false;
            if (exitButtonText != null)
                exitButtonText.text = "Exit Game";
        }

        if (isResetPressed && Time.time - lastResetClickTime > doubleClickTime)
        {
            isResetPressed = false;
            if (resetButtonText != null)
                resetButtonText.text = "Reset Progress";
        }
    }
}