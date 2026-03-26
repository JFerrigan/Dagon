using System.Collections.Generic;
using Dagon.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dagon.Bootstrap
{
    [DisallowMultipleComponent]
    public sealed class StageMenuController : MonoBehaviour
    {
        private const string BlackMireSceneName = "BlackMire";
        private const string DeveloperSandboxSceneName = "DeveloperSandbox";
        private const string MenuBackgroundResourcePath = "Sprites/UI/menu_background";
        private const string MenuLogoResourcePath = "Sprites/UI/menu_logo";
        private const string EnterResourcePath = "Sprites/UI/enterv2";
        private const string AlterationsResourcePath = "Sprites/UI/alterationsv2";
        private const string ExitResourcePath = "Sprites/UI/exitv2";

        private const string MasterVolumePrefKey = "menu.masterVolume";
        private const string FullscreenPrefKey = "menu.fullscreen";
        private const string ResolutionPrefKey = "menu.resolutionIndex";
        private const string VSyncPrefKey = "menu.vsync";
        private const string QualityPrefKey = "menu.qualityLevel";
        private const string BrightnessPrefKey = "menu.brightness";
        private const float MainMenuButtonWidth = 420f;
        private const float MainMenuButtonHeight = 88f;
        private const float AlterationsRowHeight = 46f;
        private const float AlterationsRowGap = 8f;
        private const float AlterationsFooterHeight = 56f;
        private static readonly Color MainMenuHighlightColor = new Color(0.88f, 0.82f, 0.60f, 0.18f);

        private enum MenuView
        {
            Main,
            CharacterSelect,
            Alterations
        }

        private enum MenuSelection
        {
            Enter,
            Alterations,
            Exit
        }

        private enum AlterationsSelection
        {
            MasterVolume,
            Brightness,
            Fullscreen,
            Resolution,
            VSync,
            Quality,
            Back
        }

        private readonly struct MenuOptionArt
        {
            public MenuOptionArt(Texture2D texture)
            {
                Texture = texture;
            }

            public Texture2D Texture { get; }
        }

        private readonly struct ResolutionOption
        {
            public ResolutionOption(int width, int height)
            {
                Width = width;
                Height = height;
            }

            public int Width { get; }
            public int Height { get; }

            public override string ToString()
            {
                return Width + " x " + Height;
            }
        }

        private Texture2D menuBackground;
        private Texture2D menuLogo;
        private Texture2D whiteTexture;
        private CharacterProfileDefinition[] characterProfiles;
        private Texture2D[] characterPortraits;

        private GUIStyle titleStyle;
        private GUIStyle panelStyle;
        private GUIStyle panelTitleStyle;
        private GUIStyle settingsLabelStyle;
        private GUIStyle settingsValueStyle;
        private GUIStyle settingsButtonStyle;
        private GUIStyle characterNameStyle;
        private GUIStyle characterBodyStyle;
        private GUIStyle centeredBodyStyle;

        private MenuOptionArt enterOptionArt;
        private MenuOptionArt alterationsOptionArt;
        private MenuOptionArt exitOptionArt;

        private MenuView currentView = MenuView.Main;
        private MenuSelection currentSelection = MenuSelection.Enter;
        private bool keyboardMainMenuHighlightEnabled = true;
        private AlterationsSelection currentAlterationsSelection = AlterationsSelection.MasterVolume;
        private int currentCharacterSelectionIndex;

        private List<ResolutionOption> resolutionOptions;
        private int currentResolutionIndex;
        private int currentQualityIndex;
        private float masterVolume = 0.85f;
        private float brightness = 1f;
        private bool fullscreenEnabled = true;
        private bool vSyncEnabled = true;

        private void Start()
        {
            EnsureCamera();
            menuBackground = Resources.Load<Texture2D>(MenuBackgroundResourcePath);
            menuLogo = Resources.Load<Texture2D>(MenuLogoResourcePath);
            whiteTexture = Texture2D.whiteTexture;
            enterOptionArt = new MenuOptionArt(Resources.Load<Texture2D>(EnterResourcePath));
            alterationsOptionArt = new MenuOptionArt(Resources.Load<Texture2D>(AlterationsResourcePath));
            exitOptionArt = new MenuOptionArt(Resources.Load<Texture2D>(ExitResourcePath));
            characterProfiles = RuntimeCharacterCatalog.GetCharacterProfiles();
            characterPortraits = LoadCharacterPortraits(characterProfiles);

            BuildResolutionOptions();
            LoadSettings();
            ApplyAllSettings();

            if (RunSelectionState.ConsumeOpenCharacterSelectOnMenu())
            {
                OpenCharacterSelect();
            }
        }

        private void OnGUI()
        {
            EnsureStyles();
            var previousMatrix = GUI.matrix;
            var previousColor = GUI.color;
            var previousBackground = GUI.backgroundColor;

            DrawBackground();
            GUI.color = Color.white;

            var scale = Mathf.Max(1.15f, Mathf.Min(Screen.width / 1600f, Screen.height / 900f) * 1.25f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

            var width = 760f;
            var height = 620f;
            var scaledWidth = Screen.width / scale;
            var scaledHeight = Screen.height / scale;
            var left = (scaledWidth - width) * 0.5f;
            var top = (scaledHeight - height) * 0.5f;
            var hoveredMainMenuSelection = currentView == MenuView.Main
                ? GetHoveredMainMenuSelection(left, top, width)
                : null;

            if (hoveredMainMenuSelection.HasValue)
            {
                keyboardMainMenuHighlightEnabled = false;
            }

            HandleGuiInput(Event.current, hoveredMainMenuSelection);

            if (currentView == MenuView.Main)
            {
                DrawMainMenu(left, top, width, hoveredMainMenuSelection);
            }
            else if (currentView == MenuView.CharacterSelect)
            {
                DrawCharacterSelect(left, top, width, height);
            }
            else
            {
                DrawAlterationsMenu(left, top, width, height);
            }

            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
            GUI.backgroundColor = previousBackground;
        }

        private void HandleGuiInput(Event currentEvent, MenuSelection? hoveredMainMenuSelection)
        {
            if (currentEvent == null)
            {
                return;
            }

            if (currentEvent.type == EventType.KeyDown)
            {
                if (currentView == MenuView.Main)
                {
                    HandleMainMenuInput(currentEvent, hoveredMainMenuSelection);
                }
                else if (currentView == MenuView.CharacterSelect)
                {
                    HandleCharacterSelectInput(currentEvent);
                }
                else
                {
                    HandleAlterationsInput(currentEvent);
                }
            }
            else if (currentEvent.type == EventType.ScrollWheel)
            {
                if (currentView == MenuView.Main)
                {
                    if (currentEvent.delta.y > 0f)
                    {
                        keyboardMainMenuHighlightEnabled = true;
                        MoveSelectionDown();
                        currentEvent.Use();
                    }
                    else if (currentEvent.delta.y < 0f)
                    {
                        keyboardMainMenuHighlightEnabled = true;
                        MoveSelectionUp();
                        currentEvent.Use();
                    }
                }
                else if (currentView == MenuView.CharacterSelect)
                {
                    if (currentEvent.delta.y > 0f)
                    {
                        MoveCharacterSelection(1);
                        currentEvent.Use();
                    }
                    else if (currentEvent.delta.y < 0f)
                    {
                        MoveCharacterSelection(-1);
                        currentEvent.Use();
                    }
                }
                else
                {
                    if (currentEvent.delta.y > 0f)
                    {
                        MoveAlterationsSelectionDown();
                        currentEvent.Use();
                    }
                    else if (currentEvent.delta.y < 0f)
                    {
                        MoveAlterationsSelectionUp();
                        currentEvent.Use();
                    }
                }
            }
        }

        private void HandleCharacterSelectInput(Event currentEvent)
        {
            if (currentEvent.keyCode == KeyCode.LeftArrow || currentEvent.keyCode == KeyCode.A)
            {
                MoveCharacterSelection(-1);
                currentEvent.Use();
                return;
            }

            if (currentEvent.keyCode == KeyCode.RightArrow || currentEvent.keyCode == KeyCode.D)
            {
                MoveCharacterSelection(1);
                currentEvent.Use();
                return;
            }

            if (currentEvent.keyCode == KeyCode.Escape || currentEvent.keyCode == KeyCode.Backspace)
            {
                ReturnToMainMenu();
                currentEvent.Use();
                return;
            }

            if (currentEvent.keyCode == KeyCode.Return || currentEvent.keyCode == KeyCode.KeypadEnter || currentEvent.keyCode == KeyCode.Space)
            {
                ActivateCharacterSelection();
                currentEvent.Use();
            }
        }

        private void HandleMainMenuInput(Event currentEvent, MenuSelection? hoveredMainMenuSelection)
        {
            if (currentEvent.keyCode == KeyCode.DownArrow || currentEvent.keyCode == KeyCode.S)
            {
                keyboardMainMenuHighlightEnabled = true;
                MoveSelectionDown();
                currentEvent.Use();
            }
            else if (currentEvent.keyCode == KeyCode.UpArrow || currentEvent.keyCode == KeyCode.W)
            {
                keyboardMainMenuHighlightEnabled = true;
                MoveSelectionUp();
                currentEvent.Use();
            }
            else if (currentEvent.keyCode == KeyCode.Return || currentEvent.keyCode == KeyCode.KeypadEnter || currentEvent.keyCode == KeyCode.Space)
            {
                keyboardMainMenuHighlightEnabled = true;
                currentSelection = hoveredMainMenuSelection ?? currentSelection;
                ActivateSelection(currentSelection);
                currentEvent.Use();
            }
        }

        private void HandleAlterationsInput(Event currentEvent)
        {
            if (currentEvent.keyCode == KeyCode.DownArrow || currentEvent.keyCode == KeyCode.S)
            {
                MoveAlterationsSelectionDown();
                currentEvent.Use();
                return;
            }

            if (currentEvent.keyCode == KeyCode.UpArrow || currentEvent.keyCode == KeyCode.W)
            {
                MoveAlterationsSelectionUp();
                currentEvent.Use();
                return;
            }

            if (currentEvent.keyCode == KeyCode.Escape || currentEvent.keyCode == KeyCode.Backspace)
            {
                ReturnToMainMenu();
                currentEvent.Use();
                return;
            }

            if (currentEvent.keyCode == KeyCode.LeftArrow || currentEvent.keyCode == KeyCode.A)
            {
                AdjustCurrentSetting(-1);
                currentEvent.Use();
                return;
            }

            if (currentEvent.keyCode == KeyCode.RightArrow || currentEvent.keyCode == KeyCode.D)
            {
                AdjustCurrentSetting(1);
                currentEvent.Use();
                return;
            }

            if (currentEvent.keyCode == KeyCode.Return || currentEvent.keyCode == KeyCode.KeypadEnter || currentEvent.keyCode == KeyCode.Space)
            {
                ActivateAlterationsSelection();
                currentEvent.Use();
            }
        }

        private void DrawMainMenu(float left, float top, float width, MenuSelection? hoveredMainMenuSelection)
        {
            DrawLogo(left, top, width);
            DrawMenuOption(GetMainMenuButtonRect(left, width, top, 0), MenuSelection.Enter, enterOptionArt, hoveredMainMenuSelection);
            DrawMenuOption(GetMainMenuButtonRect(left, width, top, 1), MenuSelection.Alterations, alterationsOptionArt, hoveredMainMenuSelection);
            DrawMenuOption(GetMainMenuButtonRect(left, width, top, 2), MenuSelection.Exit, exitOptionArt, hoveredMainMenuSelection);
        }

        private void DrawAlterationsMenu(float left, float top, float width, float height)
        {
            DrawLogo(left, top - 24f, width);

            var panelRect = new Rect(left + 60f, top + 190f, width - 120f, height - 160f);
            GUI.color = new Color(0.04f, 0.07f, 0.07f, 0.92f);
            GUI.Box(panelRect, GUIContent.none, panelStyle);
            GUI.color = Color.white;

            var titleRect = new Rect(panelRect.x + 24f, panelRect.y + 18f, panelRect.width - 48f, 40f);
            GUI.Label(titleRect, "ALTERATIONS", panelTitleStyle);

            var rowLeft = panelRect.x + 32f;
            var rowWidth = panelRect.width - 64f;
            var rowHeight = AlterationsRowHeight;
            var rowTop = panelRect.y + 82f;
            var rowGap = AlterationsRowGap;

            DrawMasterVolumeRow(new Rect(rowLeft, rowTop, rowWidth, rowHeight), AlterationsSelection.MasterVolume);
            DrawSliderRow(new Rect(rowLeft, rowTop + (rowHeight + rowGap), rowWidth, rowHeight), "Brightness", brightness, 0.5f, 1.5f, AlterationsSelection.Brightness, ApplyBrightnessValue, true);
            DrawToggleRow(new Rect(rowLeft, rowTop + ((rowHeight + rowGap) * 2f), rowWidth, rowHeight), "Fullscreen", fullscreenEnabled ? "On" : "Off", AlterationsSelection.Fullscreen, ToggleFullscreen);
            DrawCycleRow(new Rect(rowLeft, rowTop + ((rowHeight + rowGap) * 3f), rowWidth, rowHeight), "Resolution", GetCurrentResolutionLabel(), AlterationsSelection.Resolution, () => AdjustResolution(-1), () => AdjustResolution(1));
            DrawToggleRow(new Rect(rowLeft, rowTop + ((rowHeight + rowGap) * 4f), rowWidth, rowHeight), "V-Sync", vSyncEnabled ? "On" : "Off", AlterationsSelection.VSync, ToggleVSync);
            DrawCycleRow(new Rect(rowLeft, rowTop + ((rowHeight + rowGap) * 5f), rowWidth, rowHeight), "Quality", GetCurrentQualityLabel(), AlterationsSelection.Quality, () => AdjustQuality(-1), () => AdjustQuality(1));

            var footerRect = new Rect(panelRect.x + 24f, panelRect.yMax - AlterationsFooterHeight - 12f, panelRect.width - 48f, AlterationsFooterHeight);
            var backRect = new Rect(footerRect.x + 8f, footerRect.y + 8f, 180f, footerRect.height - 16f);
            DrawSettingsRowBackground(footerRect, currentAlterationsSelection == AlterationsSelection.Back);
            if (DrawActionButton(backRect, "Back", currentAlterationsSelection == AlterationsSelection.Back))
            {
                ReturnToMainMenu();
            }

            if (footerRect.Contains(Event.current.mousePosition))
            {
                currentAlterationsSelection = AlterationsSelection.Back;
            }
        }

        private void DrawCharacterSelect(float left, float top, float width, float height)
        {
            DrawLogo(left, top - 24f, width);

            var panelRect = new Rect(left + 24f, top + 184f, width - 48f, height - 136f);
            GUI.color = new Color(0.04f, 0.07f, 0.07f, 0.92f);
            GUI.Box(panelRect, GUIContent.none, panelStyle);
            GUI.color = Color.white;

            GUI.Label(new Rect(panelRect.x + 28f, panelRect.y + 18f, panelRect.width - 56f, 38f), "CHOOSE YOUR SURVIVOR", panelTitleStyle);
            GUI.Label(new Rect(panelRect.x + 28f, panelRect.y + 54f, panelRect.width - 56f, 22f), "Each character starts with a different weapon and signature active.", settingsValueStyle);

            if (characterProfiles == null || characterProfiles.Length == 0)
            {
                return;
            }

            const float gap = 18f;
            var cardWidth = (panelRect.width - 56f - (gap * 2f)) / 3f;
            var cardHeight = panelRect.height - 132f;
            var cardTop = panelRect.y + 92f;
            var mousePosition = Event.current != null ? Event.current.mousePosition : Vector2.zero;

            for (var index = 0; index < characterProfiles.Length; index++)
            {
                var cardLeft = panelRect.x + 28f + (index * (cardWidth + gap));
                var cardRect = new Rect(cardLeft, cardTop, cardWidth, cardHeight);
                var isSelected = index == currentCharacterSelectionIndex;
                var isHovered = cardRect.Contains(mousePosition);
                DrawCharacterCard(cardRect, characterProfiles[index], characterPortraits != null && index < characterPortraits.Length ? characterPortraits[index] : null, isSelected || isHovered);

                if (isHovered)
                {
                    currentCharacterSelectionIndex = index;
                }

                if (Event.current != null && Event.current.type == EventType.MouseDown && Event.current.button == 0 && cardRect.Contains(Event.current.mousePosition))
                {
                    currentCharacterSelectionIndex = index;
                    ActivateCharacterSelection();
                    Event.current.Use();
                }
            }

            var footerRect = new Rect(panelRect.x + 28f, panelRect.yMax - 42f, panelRect.width - 56f, 32f);
            var developerModeRect = new Rect(footerRect.xMax - 208f, footerRect.y, 208f, 32f);
            var instructionsRect = new Rect(footerRect.x, footerRect.y + 6f, footerRect.width - 224f, 20f);
            GUI.Label(instructionsRect, "Enter: Confirm    Esc: Back", settingsValueStyle);
            if (DrawActionButton(developerModeRect, "Developer Mode", false))
            {
                SceneManager.LoadScene(DeveloperSandboxSceneName);
            }
        }

        private void DrawCharacterCard(Rect rect, CharacterProfileDefinition profile, Texture2D portrait, bool highlighted)
        {
            var accent = profile != null ? profile.AccentColor : Color.white;
            GUI.color = highlighted
                ? new Color(accent.r * 0.25f + 0.12f, accent.g * 0.25f + 0.12f, accent.b * 0.25f + 0.12f, 0.96f)
                : new Color(0.08f, 0.12f, 0.11f, 0.9f);
            GUI.DrawTexture(rect, whiteTexture, ScaleMode.StretchToFill, false);

            GUI.color = highlighted
                ? new Color(accent.r, accent.g, accent.b, 0.92f)
                : new Color(0.24f, 0.32f, 0.30f, 0.92f);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 2f), whiteTexture, ScaleMode.StretchToFill, false);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), whiteTexture, ScaleMode.StretchToFill, false);
            GUI.color = Color.white;

            var portraitRect = new Rect(rect.x + 18f, rect.y + 18f, rect.width - 36f, rect.height * 0.42f);
            DrawCharacterPortrait(portraitRect, portrait, accent, highlighted);

            GUI.Label(new Rect(rect.x + 16f, portraitRect.yMax + 8f, rect.width - 32f, 26f), profile.DisplayName, characterNameStyle);
            var kitSummary = profile.StartingBaseWeapon.DisplayName + "\n" +
                profile.StartingActive.DisplayName + "\n" +
                profile.TraitSummary;
            GUI.Label(new Rect(rect.x + 16f, portraitRect.yMax + 34f, rect.width - 32f, rect.yMax - (portraitRect.yMax + 46f)), kitSummary, settingsValueStyle);
        }

        private void DrawCharacterPortrait(Rect portraitRect, Texture2D portrait, Color accent, bool highlighted)
        {
            GUI.color = new Color(0.02f, 0.02f, 0.03f, 0.98f);
            GUI.DrawTexture(portraitRect, whiteTexture, ScaleMode.StretchToFill, false);

            var borderColor = highlighted
                ? new Color(accent.r, accent.g, accent.b, 0.95f)
                : new Color(0.24f, 0.32f, 0.30f, 0.95f);
            GUI.color = borderColor;
            GUI.DrawTexture(new Rect(portraitRect.x, portraitRect.y, portraitRect.width, 2f), whiteTexture, ScaleMode.StretchToFill, false);
            GUI.DrawTexture(new Rect(portraitRect.x, portraitRect.yMax - 2f, portraitRect.width, 2f), whiteTexture, ScaleMode.StretchToFill, false);
            GUI.DrawTexture(new Rect(portraitRect.x, portraitRect.y, 2f, portraitRect.height), whiteTexture, ScaleMode.StretchToFill, false);
            GUI.DrawTexture(new Rect(portraitRect.xMax - 2f, portraitRect.y, 2f, portraitRect.height), whiteTexture, ScaleMode.StretchToFill, false);

            var innerRect = new Rect(portraitRect.x + 8f, portraitRect.y + 8f, portraitRect.width - 16f, portraitRect.height - 16f);
            GUI.color = Color.white;
            if (portrait == null || portrait.width <= 0 || portrait.height <= 0)
            {
                return;
            }

            GUI.DrawTexture(GetNativeImageRect(innerRect, portrait), portrait, ScaleMode.ScaleToFit, true);
        }

        private void DrawMasterVolumeRow(Rect rect, AlterationsSelection selection)
        {
            DrawSliderRow(rect, "Master Volume", masterVolume, 0f, 1f, selection, ApplyMasterVolumeValue, false);
        }

        private void DrawSliderRow(Rect rect, string label, float value, float minValue, float maxValue, AlterationsSelection selection, System.Action<float> applyAction, bool showMultiplier)
        {
            var isSelected = currentAlterationsSelection == selection;
            DrawSettingsRowBackground(rect, isSelected);

            var labelRect = new Rect(rect.x + 16f, rect.y + 10f, 170f, rect.height - 20f);
            GUI.Label(labelRect, label, settingsLabelStyle);

            var sliderRect = new Rect(rect.x + 192f, rect.y + 18f, 200f, 18f);
            var sliderValue = GUI.HorizontalSlider(sliderRect, value, minValue, maxValue);
            if (!Mathf.Approximately(sliderValue, value))
            {
                applyAction(sliderValue);
            }

            var valueRect = new Rect(rect.xMax - 96f, rect.y + 10f, 80f, rect.height - 20f);
            var labelValue = showMultiplier
                ? sliderValue.ToString("0.00") + "x"
                : Mathf.RoundToInt(sliderValue * 100f) + "%";
            GUI.Label(valueRect, labelValue, settingsValueStyle);

            if (rect.Contains(Event.current.mousePosition))
            {
                currentAlterationsSelection = selection;
            }
        }

        private void DrawToggleRow(Rect rect, string label, string value, AlterationsSelection selection, System.Action toggleAction)
        {
            var isSelected = currentAlterationsSelection == selection;
            DrawSettingsRowBackground(rect, isSelected);

            var labelRect = new Rect(rect.x + 16f, rect.y + 10f, 220f, rect.height - 20f);
            GUI.Label(labelRect, label, settingsLabelStyle);

            var buttonRect = new Rect(rect.xMax - 152f, rect.y + 8f, 136f, rect.height - 16f);
            if (DrawActionButton(buttonRect, value, isSelected))
            {
                toggleAction();
            }

            if (rect.Contains(Event.current.mousePosition))
            {
                currentAlterationsSelection = selection;
            }
        }

        private void DrawCycleRow(Rect rect, string label, string value, AlterationsSelection selection, System.Action decrementAction, System.Action incrementAction)
        {
            var isSelected = currentAlterationsSelection == selection;
            DrawSettingsRowBackground(rect, isSelected);

            var labelRect = new Rect(rect.x + 16f, rect.y + 10f, 170f, rect.height - 20f);
            GUI.Label(labelRect, label, settingsLabelStyle);

            var leftButtonRect = new Rect(rect.xMax - 228f, rect.y + 8f, 40f, rect.height - 16f);
            var valueRect = new Rect(rect.xMax - 184f, rect.y + 10f, 136f, rect.height - 20f);
            var rightButtonRect = new Rect(rect.xMax - 44f, rect.y + 8f, 40f, rect.height - 16f);

            if (DrawActionButton(leftButtonRect, "<", isSelected))
            {
                decrementAction();
            }

            GUI.Label(valueRect, value, settingsValueStyle);

            if (DrawActionButton(rightButtonRect, ">", isSelected))
            {
                incrementAction();
            }

            if (rect.Contains(Event.current.mousePosition))
            {
                currentAlterationsSelection = selection;
            }
        }

        private bool DrawActionButton(Rect rect, string label, bool selected)
        {
            var previousColor = GUI.color;
            if (selected)
            {
                GUI.color = new Color(1f, 0.95f, 0.72f, 1f);
            }

            var clicked = GUI.Button(rect, label, settingsButtonStyle);
            GUI.color = previousColor;
            return clicked;
        }

        private void DrawSettingsRowBackground(Rect rect, bool selected)
        {
            if (whiteTexture == null)
            {
                return;
            }

            GUI.color = selected
                ? new Color(0.14f, 0.20f, 0.18f, 0.94f)
                : new Color(0.08f, 0.12f, 0.11f, 0.88f);
            GUI.DrawTexture(rect, whiteTexture, ScaleMode.StretchToFill, false);

            var borderRect = new Rect(rect.x, rect.yMax - 1f, rect.width, 1f);
            GUI.color = selected
                ? new Color(0.94f, 0.82f, 0.38f, 0.95f)
                : new Color(0.28f, 0.34f, 0.31f, 0.9f);
            GUI.DrawTexture(borderRect, whiteTexture, ScaleMode.StretchToFill, false);
            GUI.color = Color.white;
        }

        private void MoveSelectionDown()
        {
            currentSelection = currentSelection switch
            {
                MenuSelection.Enter => MenuSelection.Alterations,
                MenuSelection.Alterations => MenuSelection.Exit,
                _ => MenuSelection.Enter
            };
        }

        private void MoveSelectionUp()
        {
            currentSelection = currentSelection switch
            {
                MenuSelection.Exit => MenuSelection.Alterations,
                MenuSelection.Alterations => MenuSelection.Enter,
                _ => MenuSelection.Exit
            };
        }

        private void MoveAlterationsSelectionDown()
        {
            currentAlterationsSelection = currentAlterationsSelection switch
            {
                AlterationsSelection.MasterVolume => AlterationsSelection.Brightness,
                AlterationsSelection.Brightness => AlterationsSelection.Fullscreen,
                AlterationsSelection.Fullscreen => AlterationsSelection.Resolution,
                AlterationsSelection.Resolution => AlterationsSelection.VSync,
                AlterationsSelection.VSync => AlterationsSelection.Quality,
                AlterationsSelection.Quality => AlterationsSelection.Back,
                _ => AlterationsSelection.MasterVolume
            };
        }

        private void MoveAlterationsSelectionUp()
        {
            currentAlterationsSelection = currentAlterationsSelection switch
            {
                AlterationsSelection.Back => AlterationsSelection.Quality,
                AlterationsSelection.Quality => AlterationsSelection.VSync,
                AlterationsSelection.VSync => AlterationsSelection.Resolution,
                AlterationsSelection.Resolution => AlterationsSelection.Fullscreen,
                AlterationsSelection.Fullscreen => AlterationsSelection.Brightness,
                AlterationsSelection.Brightness => AlterationsSelection.MasterVolume,
                _ => AlterationsSelection.Back
            };
        }

        private void ActivateSelection(MenuSelection selection)
        {
            switch (selection)
            {
                case MenuSelection.Enter:
                    OpenCharacterSelect();
                    break;
                case MenuSelection.Alterations:
                    currentView = MenuView.Alterations;
                    currentAlterationsSelection = AlterationsSelection.MasterVolume;
                    break;
                case MenuSelection.Exit:
                    Application.Quit();
                    break;
            }
        }

        private void ActivateAlterationsSelection()
        {
            switch (currentAlterationsSelection)
            {
                case AlterationsSelection.Fullscreen:
                    ToggleFullscreen();
                    break;
                case AlterationsSelection.Brightness:
                    ApplyBrightnessValue(brightness + 0.1f);
                    break;
                case AlterationsSelection.Resolution:
                    AdjustResolution(1);
                    break;
                case AlterationsSelection.VSync:
                    ToggleVSync();
                    break;
                case AlterationsSelection.Quality:
                    AdjustQuality(1);
                    break;
                case AlterationsSelection.Back:
                    ReturnToMainMenu();
                    break;
            }
        }

        private void AdjustCurrentSetting(int direction)
        {
            switch (currentAlterationsSelection)
            {
                case AlterationsSelection.MasterVolume:
                    ApplyMasterVolumeValue(masterVolume + (0.05f * direction));
                    break;
                case AlterationsSelection.Brightness:
                    ApplyBrightnessValue(brightness + (0.05f * direction));
                    break;
                case AlterationsSelection.Fullscreen:
                    ToggleFullscreen();
                    break;
                case AlterationsSelection.Resolution:
                    AdjustResolution(direction);
                    break;
                case AlterationsSelection.VSync:
                    ToggleVSync();
                    break;
                case AlterationsSelection.Quality:
                    AdjustQuality(direction);
                    break;
                case AlterationsSelection.Back:
                    if (direction > 0)
                    {
                        ReturnToMainMenu();
                    }
                    break;
            }
        }

        private void ToggleFullscreen()
        {
            fullscreenEnabled = !fullscreenEnabled;
            ApplyDisplayMode();
            SaveSettings();
        }

        private void AdjustResolution(int direction)
        {
            if (resolutionOptions == null || resolutionOptions.Count == 0)
            {
                return;
            }

            currentResolutionIndex += direction;
            if (currentResolutionIndex < 0)
            {
                currentResolutionIndex = resolutionOptions.Count - 1;
            }
            else if (currentResolutionIndex >= resolutionOptions.Count)
            {
                currentResolutionIndex = 0;
            }

            ApplyDisplayMode();
            SaveSettings();
        }

        private void ToggleVSync()
        {
            vSyncEnabled = !vSyncEnabled;
            ApplyVSync();
            SaveSettings();
        }

        private void AdjustQuality(int direction)
        {
            var qualityNames = QualitySettings.names;
            if (qualityNames == null || qualityNames.Length == 0)
            {
                return;
            }

            currentQualityIndex += direction;
            if (currentQualityIndex < 0)
            {
                currentQualityIndex = qualityNames.Length - 1;
            }
            else if (currentQualityIndex >= qualityNames.Length)
            {
                currentQualityIndex = 0;
            }

            ApplyQuality();
            SaveSettings();
        }

        private void ReturnToMainMenu()
        {
            currentView = MenuView.Main;
            currentCharacterSelectionIndex = 0;
        }

        private void OpenCharacterSelect()
        {
            currentView = MenuView.CharacterSelect;
            currentCharacterSelectionIndex = GetCharacterSelectionIndex(RunSelectionState.LastSelectedCharacterId);
        }

        private void BuildResolutionOptions()
        {
            resolutionOptions = new List<ResolutionOption>();
            var seen = new HashSet<string>();

            foreach (var resolution in Screen.resolutions)
            {
                var key = resolution.width + "x" + resolution.height;
                if (!seen.Add(key))
                {
                    continue;
                }

                resolutionOptions.Add(new ResolutionOption(resolution.width, resolution.height));
            }

            if (resolutionOptions.Count == 0)
            {
                resolutionOptions.Add(new ResolutionOption(Screen.currentResolution.width, Screen.currentResolution.height));
            }
        }

        private void LoadSettings()
        {
            masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumePrefKey, 0.85f));
            brightness = Mathf.Clamp(PlayerPrefs.GetFloat(BrightnessPrefKey, 1f), 0.5f, 1.5f);
            fullscreenEnabled = PlayerPrefs.GetInt(FullscreenPrefKey, Screen.fullScreen ? 1 : 0) == 1;
            vSyncEnabled = PlayerPrefs.GetInt(VSyncPrefKey, QualitySettings.vSyncCount > 0 ? 1 : 0) == 1;

            var qualityCount = QualitySettings.names.Length;
            currentQualityIndex = Mathf.Clamp(PlayerPrefs.GetInt(QualityPrefKey, QualitySettings.GetQualityLevel()), 0, Mathf.Max(qualityCount - 1, 0));

            currentResolutionIndex = GetSavedResolutionIndex();
        }

        private int GetSavedResolutionIndex()
        {
            if (resolutionOptions == null || resolutionOptions.Count == 0)
            {
                return 0;
            }

            var fallbackIndex = GetNearestResolutionIndex(Screen.width, Screen.height);
            var savedIndex = PlayerPrefs.GetInt(ResolutionPrefKey, fallbackIndex);
            return Mathf.Clamp(savedIndex, 0, resolutionOptions.Count - 1);
        }

        private int GetNearestResolutionIndex(int width, int height)
        {
            if (resolutionOptions == null || resolutionOptions.Count == 0)
            {
                return 0;
            }

            for (var index = 0; index < resolutionOptions.Count; index++)
            {
                var option = resolutionOptions[index];
                if (option.Width == width && option.Height == height)
                {
                    return index;
                }
            }

            return Mathf.Clamp(resolutionOptions.Count - 1, 0, resolutionOptions.Count - 1);
        }

        private void ApplyAllSettings()
        {
            ApplyMasterVolume();
            ApplyBrightness();
            ApplyDisplayMode();
            ApplyQuality();
            ApplyVSync();
        }

        private void ApplyMasterVolumeValue(float value)
        {
            masterVolume = Mathf.Clamp01(value);
            ApplyMasterVolume();
            SaveSettings();
        }

        private void ApplyMasterVolume()
        {
            AudioListener.volume = masterVolume;
        }

        private void ApplyBrightnessValue(float value)
        {
            brightness = Mathf.Clamp(value, 0.5f, 1.5f);
            ApplyBrightness();
            SaveSettings();
        }

        private void ApplyBrightness()
        {
        }

        private void ApplyDisplayMode()
        {
            if (resolutionOptions == null || resolutionOptions.Count == 0)
            {
                Screen.fullScreen = fullscreenEnabled;
                return;
            }

            var resolution = resolutionOptions[Mathf.Clamp(currentResolutionIndex, 0, resolutionOptions.Count - 1)];
            Screen.SetResolution(resolution.Width, resolution.Height, fullscreenEnabled);
        }

        private void ApplyVSync()
        {
            QualitySettings.vSyncCount = vSyncEnabled ? 1 : 0;
        }

        private void ApplyQuality()
        {
            var qualityNames = QualitySettings.names;
            if (qualityNames == null || qualityNames.Length == 0)
            {
                return;
            }

            QualitySettings.SetQualityLevel(Mathf.Clamp(currentQualityIndex, 0, qualityNames.Length - 1), true);
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetFloat(MasterVolumePrefKey, masterVolume);
            PlayerPrefs.SetFloat(BrightnessPrefKey, brightness);
            PlayerPrefs.SetInt(FullscreenPrefKey, fullscreenEnabled ? 1 : 0);
            PlayerPrefs.SetInt(ResolutionPrefKey, currentResolutionIndex);
            PlayerPrefs.SetInt(VSyncPrefKey, vSyncEnabled ? 1 : 0);
            PlayerPrefs.SetInt(QualityPrefKey, currentQualityIndex);
            PlayerPrefs.Save();
        }

        private string GetCurrentResolutionLabel()
        {
            if (resolutionOptions == null || resolutionOptions.Count == 0)
            {
                return Screen.width + " x " + Screen.height;
            }

            return resolutionOptions[Mathf.Clamp(currentResolutionIndex, 0, resolutionOptions.Count - 1)].ToString();
        }

        private string GetCurrentQualityLabel()
        {
            var qualityNames = QualitySettings.names;
            if (qualityNames == null || qualityNames.Length == 0)
            {
                return "Default";
            }

            return qualityNames[Mathf.Clamp(currentQualityIndex, 0, qualityNames.Length - 1)];
        }

        private static void EnsureCamera()
        {
            if (Camera.main != null)
            {
                return;
            }

            var cameraObject = new GameObject("Menu Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.08f, 0.08f, 1f);
            camera.transform.position = new Vector3(0f, 8f, -8f);
            camera.transform.rotation = Quaternion.Euler(30f, 0f, 0f);
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 34,
                fontStyle = FontStyle.Bold
            };
            titleStyle.normal.textColor = new Color(0.88f, 0.96f, 0.82f, 1f);

            panelStyle = new GUIStyle(GUI.skin.box);
            panelStyle.normal.background = whiteTexture;
            panelStyle.normal.textColor = Color.white;

            panelTitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 28,
                fontStyle = FontStyle.Bold
            };
            panelTitleStyle.normal.textColor = new Color(0.92f, 0.95f, 0.88f, 1f);

            settingsLabelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 20,
                fontStyle = FontStyle.Bold
            };
            settingsLabelStyle.normal.textColor = new Color(0.84f, 0.91f, 0.86f, 1f);

            settingsValueStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18
            };
            settingsValueStyle.normal.textColor = new Color(0.97f, 0.95f, 0.84f, 1f);

            settingsButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            characterNameStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24,
                fontStyle = FontStyle.Bold
            };
            characterNameStyle.normal.textColor = new Color(0.94f, 0.96f, 0.90f, 1f);

            characterBodyStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 16,
                wordWrap = true
            };
            characterBodyStyle.normal.textColor = new Color(0.80f, 0.87f, 0.82f, 1f);

            centeredBodyStyle = new GUIStyle(characterBodyStyle)
            {
                alignment = TextAnchor.MiddleCenter
            };

        }

        private void DrawBackground()
        {
            if (whiteTexture == null)
            {
                return;
            }

            if (menuBackground != null)
            {
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), menuBackground, ScaleMode.ScaleAndCrop);
            }
            else
            {
                GUI.color = new Color(0.05f, 0.08f, 0.08f, 1f);
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), whiteTexture, ScaleMode.StretchToFill, false);
            }

            DrawBrightnessOverlay();
        }

        private void DrawBrightnessOverlay()
        {
            if (whiteTexture == null || Mathf.Approximately(brightness, 1f))
            {
                return;
            }

            if (brightness < 1f)
            {
                GUI.color = new Color(0f, 0f, 0f, 1f - brightness);
            }
            else
            {
                GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01((brightness - 1f) / 0.5f) * 0.35f);
            }

            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), whiteTexture, ScaleMode.StretchToFill, false);
            GUI.color = Color.white;
        }

        private void DrawLogo(float left, float top, float width)
        {
            var logoRect = new Rect(left - 56f, top - 36f, width + 112f, 300f);
            if (menuLogo != null)
            {
                GUI.DrawTexture(logoRect, menuLogo, ScaleMode.ScaleToFit, true);
                return;
            }

            GUI.Label(logoRect, "DAGON", titleStyle);
        }

        private MenuSelection? GetHoveredMainMenuSelection(float left, float top, float width)
        {
            var currentEvent = Event.current;
            if (currentEvent == null)
            {
                return null;
            }

            var mousePosition = currentEvent.mousePosition;
            if (GetMainMenuButtonRect(left, width, top, 0).Contains(mousePosition))
            {
                return MenuSelection.Enter;
            }

            if (GetMainMenuButtonRect(left, width, top, 1).Contains(mousePosition))
            {
                return MenuSelection.Alterations;
            }

            if (GetMainMenuButtonRect(left, width, top, 2).Contains(mousePosition))
            {
                return MenuSelection.Exit;
            }

            return null;
        }

        private void DrawMenuOption(Rect rect, MenuSelection selection, MenuOptionArt optionArt, MenuSelection? hoveredMainMenuSelection)
        {
            var texture = optionArt.Texture;
            if (texture == null)
            {
                return;
            }

            var currentEvent = Event.current;
            var isHighlighted = hoveredMainMenuSelection.HasValue
                ? hoveredMainMenuSelection.Value == selection
                : keyboardMainMenuHighlightEnabled && currentSelection == selection;
            var imageRect = GetNativeImageRect(rect, texture);

            if (isHighlighted)
            {
                DrawMenuOptionHighlight(imageRect);
            }

            GUI.DrawTexture(imageRect, texture, ScaleMode.StretchToFill, true);

            if (currentEvent != null && currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && imageRect.Contains(currentEvent.mousePosition))
            {
                keyboardMainMenuHighlightEnabled = false;
                currentSelection = selection;
                ActivateSelection(selection);
                currentEvent.Use();
            }
        }

        private void DrawMenuOptionHighlight(Rect rect)
        {
            if (whiteTexture == null)
            {
                return;
            }

            var highlightRect = new Rect(rect.x - 12f, rect.y - 8f, rect.width + 24f, rect.height + 16f);
            GUI.color = MainMenuHighlightColor;
            GUI.DrawTexture(highlightRect, whiteTexture, ScaleMode.StretchToFill, false);
            GUI.color = Color.white;
        }

        private static Rect CreateButtonRect(float left, float containerWidth, float top, float width, float height)
        {
            var x = left + ((containerWidth - width) * 0.5f);
            return new Rect(x, top, width, height);
        }

        private static Rect GetMainMenuButtonRect(float left, float containerWidth, float top, int index)
        {
            var buttonsTop = top + 268f;
            var buttonsBottom = top + 564f;
            var availableButtonSpan = buttonsBottom - buttonsTop;
            var buttonGap = (availableButtonSpan - (MainMenuButtonHeight * 3f)) * 0.5f;
            var buttonTop = buttonsTop + ((MainMenuButtonHeight + buttonGap) * index);
            return CreateButtonRect(left, containerWidth, buttonTop, MainMenuButtonWidth, MainMenuButtonHeight);
        }

        private static Rect GetNativeImageRect(Rect frameRect, Texture2D texture)
        {
            if (texture == null || texture.width <= 0 || texture.height <= 0)
            {
                return frameRect;
            }

            var textureAspect = texture.width / (float)texture.height;
            var imageHeight = frameRect.height;
            var imageWidth = Mathf.Min(frameRect.width, imageHeight * textureAspect);
            if (imageWidth < frameRect.width)
            {
                var x = frameRect.x + ((frameRect.width - imageWidth) * 0.5f);
                return new Rect(x, frameRect.y, imageWidth, imageHeight);
            }

            var clampedHeight = frameRect.width / textureAspect;
            var y = frameRect.y + ((frameRect.height - clampedHeight) * 0.5f);
            return new Rect(frameRect.x, y, frameRect.width, clampedHeight);
        }

        private void MoveCharacterSelection(int direction)
        {
            if (characterProfiles == null || characterProfiles.Length == 0)
            {
                currentCharacterSelectionIndex = 0;
                return;
            }

            currentCharacterSelectionIndex += direction;
            if (currentCharacterSelectionIndex < 0)
            {
                currentCharacterSelectionIndex = characterProfiles.Length - 1;
            }
            else if (currentCharacterSelectionIndex >= characterProfiles.Length)
            {
                currentCharacterSelectionIndex = 0;
            }
        }

        private void ActivateCharacterSelection()
        {
            if (characterProfiles == null || characterProfiles.Length == 0)
            {
                SceneManager.LoadScene(BlackMireSceneName);
                return;
            }

            var selectedProfile = characterProfiles[Mathf.Clamp(currentCharacterSelectionIndex, 0, characterProfiles.Length - 1)];
            RunSelectionState.SelectCharacter(selectedProfile.CharacterId);
            SceneManager.LoadScene(BlackMireSceneName);
        }

        private int GetCharacterSelectionIndex(string characterId)
        {
            if (characterProfiles == null || characterProfiles.Length == 0 || string.IsNullOrWhiteSpace(characterId))
            {
                return 0;
            }

            for (var index = 0; index < characterProfiles.Length; index++)
            {
                if (characterProfiles[index] != null && characterProfiles[index].CharacterId == characterId)
                {
                    return index;
                }
            }

            return 0;
        }

        private static Texture2D[] LoadCharacterPortraits(CharacterProfileDefinition[] profiles)
        {
            if (profiles == null || profiles.Length == 0)
            {
                return System.Array.Empty<Texture2D>();
            }

            var textures = new Texture2D[profiles.Length];
            for (var i = 0; i < profiles.Length; i++)
            {
                if (profiles[i] == null || string.IsNullOrWhiteSpace(profiles[i].PortraitSpritePath))
                {
                    textures[i] = null;
                    continue;
                }

                textures[i] = Resources.Load<Texture2D>(profiles[i].PortraitSpritePath);
            }

            return textures;
        }
    }
}
