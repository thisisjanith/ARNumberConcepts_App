using UnityEngine;
using TMPro; // Ensure TextMeshPro is imported in your project

public class FractionQuest : MonoBehaviour
{
    [Header("Quest Settings")]
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

    void UpdateUI()
    {
        // Display fraction progress to student
        if(fractionText != null)
        {
            fractionText.text = "Restored: " + piecesCollected + " / " + totalPieces;
        }
    }

    void CompleteStone()
    {
        // Reveal whole stone and hide fragments when quest complete
        if(wholeStone != null) wholeStone.SetActive(true);

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