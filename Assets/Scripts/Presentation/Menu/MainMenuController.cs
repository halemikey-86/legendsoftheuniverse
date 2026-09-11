using System.Collections.Generic;
using LegendsOfTheUniverse.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace LegendsOfTheUniverse.Presentation.Menu
{
    [DisallowMultipleComponent]
    public class MainMenuController : MonoBehaviour
    {
        GameObject mainScreen;
        GameObject startGameScreen;
        GameObject cardDiscoveryScreen;
        GameObject optionsScreen;

        AudioSource musicSource;

        void Awake()
        {
            GameSettings.ApplyAll();
            AudioListenerBootstrap.EnsureExists();
            EnsureEventSystem();
            BuildUi();
            PlayMenuMusic();
            ShowMainScreen();
        }

        void BuildUi()
        {
            var canvas = MenuUiBuilder.CreateCanvas("MainMenuCanvas");
            MenuUiBuilder.CreateFullScreenBackground(canvas.transform, MenuUiBuilder.Background);

            mainScreen = BuildMainScreen(canvas.transform);
            startGameScreen = BuildStartGameScreen(canvas.transform);
            cardDiscoveryScreen = BuildCardDiscoveryScreen(canvas.transform);
            optionsScreen = BuildOptionsScreen(canvas.transform);
        }

        GameObject BuildMainScreen(Transform parent)
        {
            var screen = MenuUiBuilder.CreatePanel("MainScreen", parent, Vector2.zero, Vector2.one);
            MenuUiBuilder.CreateTitle(screen.transform, "Legends Of The Universe", 54f);
            MenuUiBuilder.CreateMenuButton(screen.transform, "Start Game", 0.58f, ShowStartGameScreen);
            MenuUiBuilder.CreateMenuButton(screen.transform, "Card Discovery", 0.48f, ShowCardDiscoveryScreen);
            MenuUiBuilder.CreateMenuButton(screen.transform, "Options", 0.38f, ShowOptionsScreen);
            MenuUiBuilder.CreateMenuButton(screen.transform, "Quit", 0.22f, QuitGame);
            return screen;
        }

        GameObject BuildStartGameScreen(Transform parent)
        {
            var screen = MenuUiBuilder.CreatePanel("StartGameScreen", parent, Vector2.zero, Vector2.one);
            MenuUiBuilder.CreateTitle(screen.transform, "Start Game", 48f);
            MenuUiBuilder.CreateMenuButton(screen.transform, "Solo", 0.52f, StartSoloGame);
            MenuUiBuilder.CreateMenuButton(screen.transform, "Multiplayer (Locked)", 0.42f, () => { }, interactable: false);
            MenuUiBuilder.CreateMenuButton(screen.transform, "Back", 0.22f, ShowMainScreen);
            return screen;
        }

        GameObject BuildCardDiscoveryScreen(Transform parent)
        {
            var screen = MenuUiBuilder.CreatePanel("CardDiscoveryScreen", parent, Vector2.zero, Vector2.one);
            MenuUiBuilder.CreateTitle(screen.transform, "Card Discovery", 44f);

            var scrollRoot = MenuUiBuilder.CreateRect("ScrollView", screen.transform);
            var scrollRect = scrollRoot.GetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.06f, 0.12f);
            scrollRect.anchorMax = new Vector2(0.94f, 0.72f);
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = Vector2.zero;

            var scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;

            var viewport = MenuUiBuilder.CreateRect("Viewport", scrollRoot);
            MenuUiBuilder.StretchFill(viewport);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            viewport.gameObject.AddComponent<Image>().color = new Color(0.04f, 0.06f, 0.12f, 0.5f);

            var content = MenuUiBuilder.CreateRect("Content", viewport);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);

            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(180f, 250f);
            grid.spacing = new Vector2(20f, 20f);
            grid.padding = new RectOffset(20, 20, 20, 20);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            grid.childAlignment = TextAnchor.UpperCenter;

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;

            PopulateDiscoveryGrid(content);

            MenuUiBuilder.CreateMenuButton(screen.transform, "Back", 0.06f, ShowMainScreen);
            return screen;
        }

        void PopulateDiscoveryGrid(RectTransform content)
        {
            var cards = CardCatalog.LoadDiscoveryCards();
            foreach (var texture in cards)
            {
                if (texture == null)
                    continue;

                var cell = MenuUiBuilder.CreateRect(texture.name, content);
                var cellImage = cell.gameObject.AddComponent<Image>();
                cellImage.color = new Color(0.10f, 0.12f, 0.18f, 1f);

                var art = MenuUiBuilder.CreateRect("Art", cell);
                var artRect = art.GetComponent<RectTransform>();
                artRect.anchorMin = new Vector2(0.08f, 0.22f);
                artRect.anchorMax = new Vector2(0.92f, 0.95f);
                artRect.offsetMin = Vector2.zero;
                artRect.offsetMax = Vector2.zero;

                var rawImage = art.gameObject.AddComponent<RawImage>();
                rawImage.texture = texture;
                rawImage.raycastTarget = false;

                var nameObject = MenuUiBuilder.CreateRect("Name", cell);
                var nameRect = nameObject.GetComponent<RectTransform>();
                nameRect.anchorMin = new Vector2(0.05f, 0.02f);
                nameRect.anchorMax = new Vector2(0.95f, 0.20f);
                nameRect.offsetMin = Vector2.zero;
                nameRect.offsetMax = Vector2.zero;

                var nameText = nameObject.gameObject.AddComponent<Text>();
                nameText.text = texture.name;
                nameText.font = MenuUiBuilder.DefaultFont;
                nameText.fontSize = 14;
                nameText.alignment = TextAnchor.MiddleCenter;
                nameText.color = MenuUiBuilder.TextColor;
                nameText.horizontalOverflow = HorizontalWrapMode.Wrap;
                nameText.verticalOverflow = VerticalWrapMode.Truncate;
            }
        }

        GameObject BuildOptionsScreen(Transform parent)
        {
            var screen = MenuUiBuilder.CreatePanel("OptionsScreen", parent, Vector2.zero, Vector2.one);
            if (PlaymatUiSprites.LabelSettings != null)
                MenuUiBuilder.CreateTitleSprite(screen.transform, PlaymatUiSprites.LabelSettings, new Vector2(420f, 96f));
            else
                MenuUiBuilder.CreateTitle(screen.transform, "Options", 44f);

            BuildVolumeRow(screen.transform, "Master Volume", 0.62f, GameSettings.MasterVolume, v =>
            {
                GameSettings.MasterVolume = v;
                GameSettings.ApplyAll();
                UpdateMusicVolume();
            });

            BuildVolumeRow(screen.transform, "SFX Volume", 0.52f, GameSettings.SfxVolume, v =>
            {
                GameSettings.SfxVolume = v;
                PlayerPrefs.Save();
            });

            BuildVolumeRow(screen.transform, "Music Volume", 0.42f, GameSettings.MusicVolume, v =>
            {
                GameSettings.MusicVolume = v;
                PlayerPrefs.Save();
                UpdateMusicVolume();
            });

            MenuUiBuilder.CreateLabel(screen.transform, "Display", new Vector2(0.18f, 0.34f), new Vector2(200f, 32f));
            var displayOptions = new[] { "Windowed", "Fullscreen" };
            MenuUiBuilder.CreateDropdown(screen.transform, new Vector2(0.62f, 0.34f), displayOptions, GameSettings.Fullscreen ? 1 : 0, index =>
            {
                GameSettings.Fullscreen = index == 1;
                GameSettings.ApplyAll();
            });

            MenuUiBuilder.CreateLabel(screen.transform, "Graphics Quality", new Vector2(0.18f, 0.24f), new Vector2(240f, 32f));
            MenuUiBuilder.CreateDropdown(screen.transform, new Vector2(0.62f, 0.24f), QualitySettings.names, GameSettings.QualityLevel, index =>
            {
                GameSettings.QualityLevel = index;
                GameSettings.ApplyAll();
            });

            MenuUiBuilder.CreateMenuButton(screen.transform, "Back", 0.08f, ShowMainScreen);
            return screen;
        }

        static void BuildVolumeRow(Transform parent, string label, float yAnchor, float value, UnityEngine.Events.UnityAction<float> onChanged)
        {
            MenuUiBuilder.CreateLabel(parent, label, new Vector2(0.18f, yAnchor), new Vector2(260f, 32f));
            var slider = MenuUiBuilder.CreateSlider(parent, new Vector2(0.62f, yAnchor), onChanged);
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = value;
        }

        void ShowMainScreen()
        {
            SetScreenActive(mainScreen, true);
            SetScreenActive(startGameScreen, false);
            SetScreenActive(cardDiscoveryScreen, false);
            SetScreenActive(optionsScreen, false);
        }

        void ShowStartGameScreen()
        {
            SetScreenActive(mainScreen, false);
            SetScreenActive(startGameScreen, true);
            SetScreenActive(cardDiscoveryScreen, false);
            SetScreenActive(optionsScreen, false);
        }

        void ShowCardDiscoveryScreen()
        {
            SetScreenActive(mainScreen, false);
            SetScreenActive(startGameScreen, false);
            SetScreenActive(cardDiscoveryScreen, true);
            SetScreenActive(optionsScreen, false);
        }

        void ShowOptionsScreen()
        {
            SetScreenActive(mainScreen, false);
            SetScreenActive(startGameScreen, false);
            SetScreenActive(cardDiscoveryScreen, false);
            SetScreenActive(optionsScreen, true);
        }

        static void SetScreenActive(GameObject screen, bool active)
        {
            if (screen != null)
                screen.SetActive(active);
        }

        void StartSoloGame()
        {
            SceneManager.LoadScene(MenuSceneNames.Game);
        }

        static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void PlayMenuMusic()
        {
            var clip = LoadMenuMusic();
            if (clip == null)
                return;

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.clip = clip;
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f;
            UpdateMusicVolume();
            musicSource.Play();
        }

        void UpdateMusicVolume()
        {
            if (musicSource != null)
                musicSource.volume = GameSettings.EffectiveMusicVolume;
        }

        static AudioClip LoadMenuMusic()
        {
            return GameMusic.Load(GameMusic.MainMenuTheme);
        }

        static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
                return;

            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
#else
            eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
