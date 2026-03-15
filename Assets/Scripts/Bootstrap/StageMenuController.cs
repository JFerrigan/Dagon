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
        private const string EnterResourcePath = "Sprites/UI/enter";
        private const string AlterationsResourcePath = "Sprites/UI/alterations";
        private const string ExitResourcePath = "Sprites/UI/exit";

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
        private MenuOptionArt enterOptionArt;
        private MenuOptionArt alterationsOptionArt;
        private MenuOptionArt exitOptionArt;
        private MenuSelection currentSelection = MenuSelection.Enter;
        private void Start()
        {
            EnsureCamera();
            menuBackground = Resources.Load<Texture2D>(MenuBackgroundResourcePath);
            menuLogo = Resources.Load<Texture2D>(MenuLogoResourcePath);
            whiteTexture = Texture2D.whiteTexture;
            enterOptionArt = new MenuOptionArt(Resources.Load<Texture2D>(EnterResourcePath));
            alterationsOptionArt = new MenuOptionArt(Resources.Load<Texture2D>(AlterationsResourcePath));
            exitOptionArt = new MenuOptionArt(Resources.Load<Texture2D>(ExitResourcePath));
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
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

            var width = 760f;
            var height = 620f;
            var scaledWidth = Screen.width / scale;
            var scaledHeight = Screen.height / scale;
            var left = (scaledWidth - width) * 0.5f;
            var top = (scaledHeight - height) * 0.5f;
            var buttonHeight = 88f;
            var buttonsTop = top + 268f;
            var buttonsBottom = top + 564f;
            var availableButtonSpan = buttonsBottom - buttonsTop;
            var buttonGap = (availableButtonSpan - (buttonHeight * 3f)) * 0.5f;

            DrawLogo(left, top, width);
            DrawMenuOption(CreateButtonRect(left, width, buttonsTop, buttonHeight, enterOptionArt), MenuSelection.Enter, enterOptionArt);
            DrawMenuOption(CreateButtonRect(left, width, buttonsTop + buttonHeight + buttonGap, buttonHeight, alterationsOptionArt), MenuSelection.Alterations, alterationsOptionArt);
            DrawMenuOption(CreateButtonRect(left, width, buttonsTop + ((buttonHeight + buttonGap) * 2f), buttonHeight, exitOptionArt), MenuSelection.Exit, exitOptionArt);

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

            var guiMousePosition = GetGuiMousePosition();
            var imageRect = GetScaleToFitRect(rect, texture);

            if (imageRect.Contains(guiMousePosition))
            {
                currentSelection = selection;
            }

            var isHovered = currentSelection == selection;

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && imageRect.Contains(guiMousePosition))
            {
                currentSelection = selection;
                ActivateSelection(selection);
                Event.current.Use();
            }

            if (isHovered)
            {
                DrawHoverFrame(imageRect);
            }

            GUI.DrawTexture(imageRect, texture, ScaleMode.StretchToFill, true);
        }

        private void DrawHoverFrame(Rect rect)
        {
            if (whiteTexture == null)
            {
                return;
            }

            var outer = new Rect(rect.x - 4f, rect.y - 4f, rect.width + 8f, rect.height + 8f);
            var inner = new Rect(rect.x - 1f, rect.y - 1f, rect.width + 2f, rect.height + 2f);

            GUI.color = new Color(0.86f, 0.70f, 0.18f, 0.12f);
            GUI.DrawTexture(outer, whiteTexture, ScaleMode.StretchToFill, false);
            GUI.color = new Color(0.98f, 0.86f, 0.34f, 0.2f);
            GUI.DrawTexture(inner, whiteTexture, ScaleMode.StretchToFill, false);
            GUI.color = Color.white;
        }

        private Vector2 GetGuiMousePosition()
        {
            return GUI.matrix.inverse.MultiplyPoint3x4(Event.current.mousePosition);
        }

        private static Rect CreateButtonRect(float left, float containerWidth, float top, float height, MenuOptionArt optionArt)
        {
            var texture = optionArt.Texture;
            if (texture == null || texture.height <= 0)
            {
                return new Rect(left, top, containerWidth, height);
            }

            var width = height * (texture.width / (float)texture.height);
            var x = left + ((containerWidth - width) * 0.5f);
            return new Rect(x, top, width, height);
        }

        private static Rect GetScaleToFitRect(Rect bounds, Texture2D texture)
        {
            if (texture == null || texture.width <= 0 || texture.height <= 0)
            {
                return bounds;
            }

            var textureAspect = texture.width / (float)texture.height;
            var boundsAspect = bounds.width / bounds.height;

            if (textureAspect > boundsAspect)
            {
                var height = bounds.width / textureAspect;
                var y = bounds.y + ((bounds.height - height) * 0.5f);
                return new Rect(bounds.x, y, bounds.width, height);
            }

            var width = bounds.height * textureAspect;
            var x = bounds.x + ((bounds.width - width) * 0.5f);
            return new Rect(x, bounds.y, width, bounds.height);
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
