using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dagon.Bootstrap
{
    [DisallowMultipleComponent]
    public sealed class StartupController : MonoBehaviour
    {
        private const string MainMenuSceneName = "MainMenu";

        [SerializeField] private float menuLoadDelay = 0.05f;

        private float loadTimer;
        private bool loadRequested;

        private void Awake()
        {
            EnsureCamera();
            Time.timeScale = 1f;
            loadTimer = Mathf.Max(0f, menuLoadDelay);
        }

        private void Update()
        {
            if (loadRequested)
            {
                return;
            }

            loadTimer -= Time.unscaledDeltaTime;
            if (loadTimer > 0f)
            {
                return;
            }

            loadRequested = true;
            SceneManager.LoadScene(MainMenuSceneName);
        }

        private static void EnsureCamera()
        {
            if (Camera.main != null)
            {
                return;
            }

            var cameraObject = new GameObject("Startup Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.07f, 0.07f, 1f);
            camera.transform.position = new Vector3(0f, 6f, -6f);
            camera.transform.rotation = Quaternion.Euler(30f, 0f, 0f);
        }

        private void OnGUI()
        {
            var previousMatrix = GUI.matrix;
            var scale = Mathf.Max(1.2f, Mathf.Min(Screen.width / 1280f, Screen.height / 720f));
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

            var width = 420f;
            var height = 110f;
            var scaledWidth = Screen.width / scale;
            var scaledHeight = Screen.height / scale;
            var left = (scaledWidth - width) * 0.5f;
            var top = (scaledHeight - height) * 0.5f;

            GUI.Box(new Rect(left, top, width, height), "Dagon");
            GUI.Label(new Rect(left + 24f, top + 44f, width - 48f, 24f), "Loading...");

            GUI.matrix = previousMatrix;
        }
    }
}
