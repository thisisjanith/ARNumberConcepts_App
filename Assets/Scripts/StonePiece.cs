using UnityEngine;
using System.Collections;

public class StonePiece : MonoBehaviour
{
    private FractionQuest logic;
    private bool isMoving = false;
    
    [Header("Movement & Alignment")]
    public Transform specificTarget; // Assign Target_Q1, Q2, etc. here in the Inspector
    public float flySpeed = 5f;
    public float rotationSpeed = 10f;

    void Start()
    {
        // Finding the main game logic in the parent
        logic = GetComponentInParent<FractionQuest>();
    }

    void OnMouseDown()
    {
        // Only start moving if we aren't already moving and have a target
        if (!isMoving && specificTarget != null)
        {
            StartCoroutine(FlyAndJoin());
            // Haptic feedback when the child taps the stone piece
            HapticFeedback.LightTap();
        }
    }

    IEnumerator FlyAndJoin()
    {
        isMoving = true;

        // Move and Rotate at the same time to look like "Joining"
        while (Vector3.Distance(transform.position, specificTarget.position) > 0.001f)
        {
            // Move toward the target slot
            transform.position = Vector3.MoveTowards(transform.position, specificTarget.position, flySpeed * Time.deltaTime);
            
            // Smoothly rotate to match the target's rotation
            transform.rotation = Quaternion.Slerp(transform.rotation, specificTarget.rotation, rotationSpeed * Time.deltaTime);
            
            yield return null; 
        }

        // 1. Final Snap for perfect math alignment
        transform.position = specificTarget.position;
        transform.rotation = specificTarget.rotation;

        // 2. Update the Backend Logic (FractionQuest)
        if(logic != null)
        {
            logic.AddPiece();
        }

        // Haptic success feedback when the piece locks into place
        HapticFeedback.Success();

        // 3. Update the Mission Logic (This triggers the Finish Button)
        MissionLogic mission = FindObjectOfType<MissionLogic>();
        if (mission != null)
        {
            mission.PiecePlaced();
        }
        else
        {
            Debug.LogError("MissionLogic script not found on any object! Is your LogicManager in the scene?");
        }

        // 4. Update the Visual HUD (The 0/4 counter)
        FractionHUD hud = FindObjectOfType<FractionHUD>();
        if (hud != null)
        {
            hud.AddPiece();
        }
        else
        {
            Debug.LogWarning("FractionHUD not found in the scene! Make sure your Canvas has the HUD script.");
        }
    }
}