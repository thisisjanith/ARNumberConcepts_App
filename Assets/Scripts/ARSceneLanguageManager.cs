using UnityEngine;

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

    void Start()
    {
        // Retrieve language choice from previous scene
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