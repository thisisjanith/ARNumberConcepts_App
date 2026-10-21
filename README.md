# AR Number Concepts

A bilingual (English / Sinhala) **augmented-reality learning app for children** that teaches number concepts through story-driven AR missions and drag-and-drop quizzes. Learners scan printed story-book markers with a phone camera, and 3D characters and objects come to life on the page.

Built with **Unity 2022.3 LTS** and **Vuforia Engine** for Android and iOS.

---

## Features

### AR missions (marker-based)
Each story-book page is an image target. Scanning it spawns an interactive 3D scene with narration and subtitles.

| Mission | Concept | What the child does |
|---|---|---|
| **Seed Pile** | Place value (thousands, hundreds, tens, ones) | Taps seed containers to build a target number such as 2,305. A live equation display (`2,000 + 300 + 0 + 5`) shows how the number is composed. |
| **Sun-Stone** | Fractions | Collects stone pieces scattered in AR. A circular progress HUD shows `n/4` until the whole stone is revealed. |
| **Canyon Hopper** | Even and odd numbers / number order | Guides the character Dinal across stone tiles. Correct taps make Dinal hop, and wrong taps trigger a shake. |

### Quizzes
- **Gate, River and Canyon quiz rooms**, loaded by a master controller depending on the lesson chosen.
- **Fraction matching**: drag fractions (for example `1/2`, `3/4`) onto the matching slot.
- **Even/Odd sorting**: drag numbers into the Even or Odd category.
- Instant visual feedback: slot highlighting, snap-back on wrong drops, success panels and a victory sequence.

### Learning experience
- **Bilingual UI and voice-over**: English and Sinhala, selected at login. The choice carries across all scenes.
- **Narration with typewriter subtitles**, paged to fit the subtitle box.
- **Haptic feedback** on taps and correct and incorrect actions.
- **Onboarding**: a splash and loading scene that pre-warms the AR scene, followed by a first-time tutorial carousel.
- **Main menu** with *Scan* (AR) and *Lessons* (quiz) entry points.

### Analytics
A persistent `AnalyticsManager` records, for each session:
- Language, AR time, quiz time, number of questions, errors and retries.
- Data is sent to **Firebase Realtime Database** over its REST API, with a local **CSV backup** on the device.
- `Tools/seed_firebase_sessions.js` seeds sample sessions for dashboard testing.

---

## Tech stack

| Area | Technology |
|---|---|
| Engine | Unity **2022.3.62f3** (LTS) |
| Language | C# |
| AR | **Vuforia Engine 11.4.4** (image targets, database in `StreamingAssets/Vuforia`) |
| UI | Unity UI (uGUI), **TextMeshPro**, Skyden Games *Free Casual GUI* kit |
| 3D assets | glTF/GLB models, imported with **glTFast 6.14.1** (character and tile models generated with Meshy AI) |
| Vector graphics | Unity Vector Graphics |
| Audio | MP3/WAV voice-over and narration in English and Sinhala |
| Backend / analytics | **Firebase Realtime Database** (REST via `UnityWebRequest`) and local CSV |
| Tooling | Node.js script for Firebase seed data |
| Version control | Git with **Git LFS** (Vuforia package `.tgz`) |
| Platforms | Android and iOS |

---

## Project structure

```
Assets/
├── Scenes/        OnboardingScene, LoginScene, MainMenu, SampleScene (AR), QuizScene
├── Scripts/       Game logic (missions, quizzes, UI, analytics, haptics)
├── Models/        GLB 3D assets (including Canyon/)
├── Audio/         Narration and feedback, English + Sinhala
├── Images/        UI art and quiz graphics
├── Editor/Vuforia Image target textures
├── Resources/     VuforiaConfiguration
└── StreamingAssets/Vuforia/   Image target database
Tools/             seed_firebase_sessions.js
```

### Key scripts

| Script | Role |
|---|---|
| `OnboardingLoader` | Pre-loads the AR scene while a splash is shown |
| `LanguageSwitcher`, `ARSceneLanguageManager` | English/Sinhala selection and per-scene localisation |
| `UIManager`, `TutorialCarousel` | Login, menu and first-time tutorial flow |
| `SeedPileController`, `ContainerTapHandler` | Place-value mission |
| `FractionQuest`, `StonePiece`, `FractionHUD` | Fraction mission |
| `CanyonLogic`, `CanyonTile`, `DinalHopper` | Canyon mission |
| `MasterQuizController`, `UniversalQuizManager`, `QuizManager` | Quiz orchestration |
| `QuizDraggable`, `QuizSlot`, `CanyonNumberDraggable`, `CanyonCategorySlot` | Drag-and-drop mechanics |
| `VictoryManager`, `ARToLessonTransitionManager` | Completion and AR-to-quiz transitions |
| `AnalyticsManager` | Session analytics (Firebase and CSV) |
| `HapticFeedback` | Centralised haptics |

---

## Getting started

### Prerequisites
- Unity Hub with **Unity 2022.3.62f3** and the Android and/or iOS build modules
- [Git LFS](https://git-lfs.com) (`git lfs install`)
- A free [Vuforia developer licence key](https://developer.vuforia.com)
- A physical device with a camera (AR does not work in the editor without a webcam)

### Setup
```bash
git clone https://github.com/thisisjanith/ARNumberConcepts_App.git
cd ARNumberConcepts_App
git lfs pull
```
1. Open the folder in Unity Hub.
2. Open **Vuforia Engine → Configuration** and paste your Vuforia license key.
3. Select the `AnalyticsManager` object and set **Firebase Database URL**, for example `https://<project-id>-default-rtdb.firebaseio.com/`. Leave it unset to rely on the local CSV only.
4. Open `Assets/Scenes/OnboardingScene.unity`, which is the first scene in the build.
5. **File → Build Settings**, choose Android or iOS, then **Build and Run**.

### Printing the markers
The image targets live in `Assets/Editor/Vuforia/ImageTargetTextures/Number_Concepts_StoryBook/`. Print them and point the camera at them to trigger each mission.

---

## Credits
- Vuforia Engine by PTC
- *Free Casual GUI* by Skyden Games
- Emoji art from EmojiOne
- 3D models generated with Meshy AI

## Author
**Janith Kavinda**: [@thisisjanith](https://github.com/thisisjanith)
