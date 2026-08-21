using UnityEngine;

public class LanguageSwitcher : MonoBehaviour
{
    // Static language state persists across scenes: 0 = English, 1 = Sinhala
    public static int SelectedLanguageID; 

    [Header("Panels")]
    public GameObject languagePanel;
    public GameObject loginPanel;

    [Header("Main Content Groups")]
    public GameObject englishGroup; 
    public GameObject sinhalaGroup; 

    [Header("Login Button Text")]
    public GameObject letsGoTextEng;
    public GameObject letsGoTextSin;

    [Header("Main Menu Button Texts")]
    public GameObject scanBtnEng;
    public GameObject scanBtnSin;
    public GameObject allLessonsBtnEng;
    public GameObject allLessonsBtnSin;

    [Header("Tutorial Button Texts")]
    public GameObject nextBtnEng;
    public GameObject nextBtnSin;
    public GameObject finalStartBtnEng;
    public GameObject finalStartBtnSin;

    [Header("Tutorial Step Instructions")]
    public GameObject step1Eng;
    public GameObject step1Sin;
    public GameObject step2Eng;
    public GameObject step2Sin;
    public GameObject step3Eng;
    public GameObject step3Sin;

    public void SelectLanguage(int langIndex)
    {
        SelectedLanguageID = langIndex;
        bool isEnglish = (langIndex == 0);

        // Hide language picker, show login panel
        if(languagePanel) languagePanel.SetActive(false);
        if(loginPanel) loginPanel.SetActive(true);

        // Toggle all bilingual UI elements based on selection
        if(englishGroup) englishGroup.SetActive(isEnglish);
        if(sinhalaGroup) sinhalaGroup.SetActive(!isEnglish);
        if(letsGoTextEng) letsGoTextEng.SetActive(isEnglish);
        if(letsGoTextSin) letsGoTextSin.SetActive(!isEnglish);
        if(scanBtnEng) scanBtnEng.SetActive(isEnglish);
        if(scanBtnSin) scanBtnSin.SetActive(!isEnglish);
        if(allLessonsBtnEng) allLessonsBtnEng.SetActive(isEnglish);
        if(allLessonsBtnSin) allLessonsBtnSin.SetActive(!isEnglish);
        if(nextBtnEng) nextBtnEng.SetActive(isEnglish);
        if(nextBtnSin) nextBtnSin.SetActive(!isEnglish);
        if(finalStartBtnEng) finalStartBtnEng.SetActive(isEnglish);
        if(finalStartBtnSin) finalStartBtnSin.SetActive(!isEnglish);
        if(step1Eng) step1Eng.SetActive(isEnglish);
        if(step1Sin) step1Sin.SetActive(!isEnglish);
        if(step2Eng) step2Eng.SetActive(isEnglish);
        if(step2Sin) step2Sin.SetActive(!isEnglish);
        if(step3Eng) step3Eng.SetActive(isEnglish);
        if(step3Sin) step3Sin.SetActive(!isEnglish);

        Debug.Log("Language saved and UI updated.");
    }
}