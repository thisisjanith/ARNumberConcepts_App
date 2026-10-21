using UnityEngine;
using TMPro;
using System.Collections;

public class VictoryManager : MonoBehaviour
{
    [Header("UI References")]
    // Panel containing victory sequence UI
    public GameObject successPanel;
    // Text display for typewriter victory narration
    public TMP_Text storyTextDisplay;

    [Header("Audio")]
    // Audio source for victory voiceover
    public AudioSource voiceOverSource;
    // English victory narration clip
    public AudioClip victoryClipEng;
    // Sinhala victory narration clip
    public AudioClip victoryClipSin;

    [Header("Settings")]
    // Speed of character-by-character text display (used only for English)
    public float typingSpeed = 0.05f;
    // Victory narration text (English version)
    [TextArea(3, 10)]
    public string storyContent = "Amazing work! By mastering the numbers, you have unlocked the legendary Wild Gate. Look! Dinal is stepping through the portal into the deep jungle beyond.";

    // Language state: true = Sinhala, false = English
    private bool isSinhala;

    void Start()
    {
        // Detect language selection from previous scene
        isSinhala = (LanguageSwitcher.SelectedLanguageID == 1);
    }

    // Call this from your Next Chapter button's OnClick event
    public void StartVictorySequence()
    {
        // Show success panel and begin narration
        if (successPanel) successPanel.SetActive(true);
        StartCoroutine(PlaySequence());
    }

    IEnumerator PlaySequence()
    {
        // Clear text before starting
        storyTextDisplay.text = "";
        // Select audio clip based on language
        AudioClip activeClip = isSinhala ? victoryClipSin : victoryClipEng;

        if (voiceOverSource != null && activeClip != null)
        {
            // Play voiceover
            voiceOverSource.clip = activeClip;
            voiceOverSource.Play();

            // English: typewriter effect character-by-character
            if (!isSinhala)
            {
                foreach (char c in storyContent.ToCharArray())
                {
                    storyTextDisplay.text += c;
                    yield return new WaitForSeconds(typingSpeed);
                }
            }
            else
            {
                // Sinhala: display full text instantly for readability
                storyTextDisplay.text = "විශිෂ්ටයි! ඔබ අංක ඉගෙන ගැනීමෙන් වනගත දොරටුව විවෘත කළා. බලන්න! නිමල් දැන් කැලෑවට ඇතුළු වෙනවා.";
            }
        }
    }
}