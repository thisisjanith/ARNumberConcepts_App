using UnityEngine;

/// <summary>
/// Applies the language chosen on the login screen to the AR scene's UI. Every bilingual text
/// exists twice (an English GameObject and a Sinhala GameObject); this script simply enables
/// one and disables the other, so each language can have its own font and layout.
/// </summary>
public class ARSceneLanguageManager : MonoBehaviour
{
    [Header("Subtitle Boxes to Hide (Sinhala Only)")]
    public GameObject subtitleBoxMain;  // Drag 'SubtitleBox' from Canvas here
    public GameObject successPanelImage; // Drag 'Image' from SuccessPanel here

    [Header("Victory Screen Texts")]
    public GameObject headlineEng;
    public GameObject headlineSin;

    [Header("Victory Button Texts")]
    public GameObject scanAgainEng;
    public GameObject scanAgainSin;
    public GameObject goHomeEng;
    public GameObject goHomeSin;

    /// <summary>Runs once when the AR scene loads.</summary>
    void Start()
    {
        // Retrieve language choice from previous scene
        // LanguageSwitcher.SelectedLanguageID is static, so it survives scene changes (0 = English, 1
        // = Sinhala).
        bool isSinhala = (LanguageSwitcher.SelectedLanguageID == 1);
        bool isEnglish = !isSinhala;

        // Sinhala UI omits subtitle boxes for cleaner display
        if (isSinhala)
        {
            if(subtitleBoxMain) subtitleBoxMain.SetActive(false);
            if(successPanelImage) successPanelImage.SetActive(false);
        }

        // Toggle victory screen text based on language
        if(headlineEng) headlineEng.SetActive(isEnglish);
        if(headlineSin) headlineSin.SetActive(isSinhala);

        if(scanAgainEng) scanAgainEng.SetActive(isEnglish);
        if(scanAgainSin) scanAgainSin.SetActive(isSinhala);

        if(goHomeEng) goHomeEng.SetActive(isEnglish);
        if(goHomeSin) goHomeSin.SetActive(isSinhala);
    }
}