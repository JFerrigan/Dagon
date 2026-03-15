using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Dagon.Bootstrap
{
    [DisallowMultipleComponent]
    public sealed class StageMenuController : MonoBehaviour
    {
        private const string BlackMireSceneName = "BlackMire";
        private const string MenuBackgroundResourcePath = "Sprites/UI/menu_background";
        private const string MenuLogoResourcePath = "Sprites/UI/menu_logo";
        private const string EnterUnselectedResourcePath = "Sprites/UI/menu_enter_unselected";
        private const string AlterationsUnselectedResourcePath = "Sprites/UI/menu_alterations_unselected";
        private const string ExitUnselectedResourcePath = "Sprites/UI/menu_exit_unselected";

        private enum MenuSelection
        {
            Enter,
            Alterations,
            Exit
        }

        private readonly struct MenuOptionArt
        {
            public MenuOptionArt(Texture2D texture)
            {
                Texture = texture;
            }

            public Texture2D Texture { get; }
        }

        private Texture2D menuBackground;
        private Texture2D menuLogo;
        private Texture2D whiteTexture;
        private GUIStyle titleStyle;
        private GUIStyle footerStyle;
        private MenuOptionArt enterOptionArt;
        private MenuOptionArt alterationsOptionArt;
        private MenuOptionArt exitOptionArt;
        private MenuSelection currentSelection = MenuSelection.Enter;
        private float currentGuiScale = 1f;

        private void Start()
        {
            EnsureCamera();
            menuBackground = Resources.Load<Texture2D>(MenuBackgroundResourcePath);
            menuLogo = Resources.Load<Texture2D>(MenuLogoResourcePath);
            whiteTexture = Texture2D.whiteTexture;
            enterOptionArt = new MenuOptionArt(Resources.Load<Texture2D>(EnterUnselectedResourcePath));
            alterationsOptionArt = new MenuOptionArt(Resources.Load<Texture2D>(AlterationsUnselectedResourcePath));
            exitOptionArt = new MenuOptionArt(Resources.Load<Texture2D>(ExitUnselectedResourcePath));
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            var wheel = mouse != null ? mouse.scroll.ReadValue().y : 0f;

            var moveDown = (keyboard != null && (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)) || wheel < -0.01f;
            var moveUp = (keyboard != null && (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)) || wheel > 0.01f;
            var activate = keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame);

            if (moveDown)
            {
                currentSelection = currentSelection switch
                {
                    MenuSelection.Enter => MenuSelection.Alterations,
                    MenuSelection.Alterations => MenuSelection.Exit,
                    _ => MenuSelection.Enter
                };
            }
            else if (moveUp)
            {
                currentSelection = currentSelection switch
                {
                    MenuSelection.Exit => MenuSelection.Alterations,
                    MenuSelection.Alterations => MenuSelection.Enter,
                    _ => MenuSelection.Exit
                };
            }

            if (activate)
            {
                ActivateSelection(currentSelection);
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
            currentGuiScale = scale;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

            var width = 760f;
            var height = 620f;
            var scaledWidth = Screen.width / scale;
            var scaledHeight = Screen.height / scale;
            var left = (scaledWidth - width) * 0.5f;
            var top = (scaledHeight - height) * 0.5f;

            DrawLogo(left, top, width);
            DrawMenuOption(new Rect(left + 56f, top + 252f, width - 112f, 102f), MenuSelection.Enter, enterOptionArt);
            DrawMenuOption(new Rect(left + 56f, top + 370f, width - 112f, 102f), MenuSelection.Alterations, alterationsOptionArt);
            DrawMenuOption(new Rect(left + 56f, top + 488f, width - 112f, 102f), MenuSelection.Exit, exitOptionArt);

            GUI.Label(new Rect(left + 48f, top + 598f, width - 96f, 20f), "Arrow keys, W/S, mouse wheel, Enter", footerStyle);

            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
            GUI.backgroundColor = previousBackground;
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

            footerStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12
            };
            footerStyle.normal.textColor = new Color(0.68f, 0.74f, 0.68f, 0.95f);
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

            GUI.color = new Color(0.02f, 0.04f, 0.04f, menuBackground != null ? 0.34f : 0.2f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), whiteTexture, ScaleMode.StretchToFill, false);
        }

        private void DrawLogo(float left, float top, float width)
        {
            var logoRect = new Rect(left - 20f, top - 8f, width + 40f, 240f);
            if (menuLogo != null)
            {
                GUI.DrawTexture(logoRect, menuLogo, ScaleMode.ScaleToFit, true);
                return;
            }

            GUI.Label(logoRect, "DAGON", titleStyle);
        }

        private void DrawMenuOption(Rect rect, MenuSelection selection, MenuOptionArt optionArt)
        {
            var texture = optionArt.Texture;
            if (texture == null)
            {
                return;
            }

            var scaledMousePosition = currentGuiScale > 0f ? Event.current.mousePosition / currentGuiScale : Event.current.mousePosition;

            if (rect.Contains(scaledMousePosition))
            {
                currentSelection = selection;
            }

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && rect.Contains(scaledMousePosition))
            {
                currentSelection = selection;
                ActivateSelection(selection);
                Event.current.Use();
            }

            GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit, true);
        }

        private static void ActivateSelection(MenuSelection selection)
        {
            switch (selection)
            {
                case MenuSelection.Enter:
                    SceneManager.LoadScene(BlackMireSceneName);
                    break;
                case MenuSelection.Alterations:
                    break;
                case MenuSelection.Exit:
                    Application.Quit();
                    break;
            }
        }
    }
}
