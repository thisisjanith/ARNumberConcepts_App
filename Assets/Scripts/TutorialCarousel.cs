using UnityEngine;
using TMPro; 
using UnityEngine.UI;

/// <summary>
/// Step-by-step tutorial (carousel) shown to first-time users in LoginScene. It shows one
/// instruction page at a time; on the last page the Next button is replaced by a Start button
/// that launches the AR scene.
/// </summary>
public class TutorialCarousel : MonoBehaviour
{
    [Header("Hierarchy Links")]
    // Array of instruction page GameObjects to cycle through
    public GameObject[] instructionPages;
    // Button to advance to next instruction page
    public Button nextButton;
    // Button shown on final page to start the AR scene
    public Button finalStartButton;

    // Tracks current page index (0-based)
    private int currentIndex = 0;

    /// <summary>
    /// OnEnable runs every time the panel is shown (not just once), so the tutorial always restarts
    /// at page 1.
    /// </summary>
    void OnEnable()
    {
        // Reset to first page when tutorial panel becomes active
        currentIndex = 0;
        ShowPage(currentIndex);
    }

    /// <summary>Wired to the Next button.</summary>
    public void OnNextPressed()
    {
        // Advance to next page
        currentIndex++;

        // Check if more pages exist
        if (currentIndex < instructionPages.Length)
        {
            ShowPage(currentIndex);
        }
        else
        {
            // All pages shown; finish tutorial
            FinishTutorial();
        }
    }

    /// <summary>Shows only the page at 'index' and swaps Next/Start on the last page.</summary>
    void ShowPage(int index)
    {
        // Safety check: prevent out-of-bounds access
        if (index >= instructionPages.Length) return;

        // Hide all pages, show only the current one
        for (int i = 0; i < instructionPages.Length; i++)
        {
            instructionPages[i].SetActive(i == index);
        }

        // On final page: hide next button, show start button
        if (index == instructionPages.Length - 1)
        {
            nextButton.gameObject.SetActive(false);
            finalStartButton.gameObject.SetActive(true);
        }
        else
        {
            // On earlier pages: show next button, hide start button
            nextButton.gameObject.SetActive(true);
            finalStartButton.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Wired to the final Start button: remembers the tutorial was seen and opens the AR scene.
    /// </summary>
    public void FinishTutorial()
    {
        // Mark tutorial as seen so it won't repeat on next login
        PlayerPrefs.SetInt("HasSeenTutorial", 1);
        PlayerPrefs.Save();

        // Find UIManager to trigger AR scene load
        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            uiManager.LoadARScene();
        }
        else
        {
            Debug.LogError("UIManager not found in the scene! Make sure it is on your LoginPanel.");
            // Fallback: hide tutorial panel if scene load fails
            gameObject.SetActive(false);
        }
    }
}