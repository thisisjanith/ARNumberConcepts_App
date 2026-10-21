using UnityEngine;
using TMPro; // Ensure TextMeshPro is imported in your project

/// <summary>
/// Gate chapter (fractions). Counts how many of the 4 Sun-Stone quarter pieces have been
/// joined. When all pieces are placed, the fragments are hidden and the whole stone is shown,
/// illustrating that 4/4 = 1 whole. It sits on the parent object of the stone pieces, and each
/// StonePiece calls AddPiece().
/// </summary>
public class FractionQuest : MonoBehaviour
{
    [Header("Quest Settings")]
    // Public so progress can be watched in the Inspector while testing.
    public int piecesCollected = 0;
    public int totalPieces = 4;
    
    [Header("References")]
    public GameObject wholeStone; // Drag your solid, non-fragmented Sun-Stone here
    public TextMeshProUGUI fractionText; // Drag your Screen UI Text here

    void Start()
    {
        // Target 60 FPS for smooth AR tracking on mobile
        Application.targetFrameRate = 60;

        // Hide whole stone until all pieces collected
        if(wholeStone != null) wholeStone.SetActive(false);
        if(fractionText != null) fractionText.gameObject.SetActive(false);

        UpdateUI();
    }

    /// <summary>Called by StonePiece when a piece snaps into place.</summary>
    public void AddPiece()
    {
        // Increment collected pieces and check for completion
        piecesCollected++;
        UpdateUI();

        if (piecesCollected >= totalPieces)
        {
            CompleteStone();
        }
    }

    /// <summary>Shows the "Restored: x / 4" progress text.</summary>
    void UpdateUI()
    {
        // Display fraction progress to student
        if(fractionText != null)
        {
            fractionText.text = "Restored: " + piecesCollected + " / " + totalPieces;
        }
    }

    /// <summary>Swaps the four fragments for the single whole stone.</summary>
    void CompleteStone()
    {
        // Reveal whole stone and hide fragments when quest complete
        if(wholeStone != null) wholeStone.SetActive(true);

        // Hide every child fragment except the whole stone and the portal well, which must stay
        // visible for the next part of the story.
        foreach (Transform child in transform)
        {
            if(child.gameObject != wholeStone && !child.name.Contains("Portal_Well"))
            {
                child.gameObject.SetActive(false);
            }
        }

        Debug.Log("The Sun-Stone is Whole! 4/4 = 1");
    }
}