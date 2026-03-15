using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dagon.Bootstrap
{
    [DisallowMultipleComponent]
    public sealed class StageMenuController : MonoBehaviour
    {
        private const string BlackMireSceneName = "BlackMire";
        private const string MireColossusBossSceneName = "MireColossusBoss";

        private void Start()
        {
            EnsureCamera();
        }

        private void OnGUI()
        {
            var previousMatrix = GUI.matrix;
            var scale = Mathf.Max(1.35f, Mathf.Min(Screen.width / 1280f, Screen.height / 720f));
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

            var width = 520f;
            var height = 320f;
            var scaledWidth = Screen.width / scale;
            var scaledHeight = Screen.height / scale;
            var left = (scaledWidth - width) * 0.5f;
            var top = (scaledHeight - height) * 0.5f;

            GUI.Box(new Rect(left, top, width, height), "Dagon");
            GUI.Label(new Rect(left + 28f, top + 42f, width - 56f, 32f), "Enter the black mire.");
            GUI.Label(new Rect(left + 28f, top + 76f, width - 56f, 72f), "Start a run from level one or jump directly to the boss stage.");

            if (GUI.Button(new Rect(left + 28f, top + 160f, width - 56f, 52f), "Start Run"))
            {
                SceneManager.LoadScene(BlackMireSceneName);
            }

            if (GUI.Button(new Rect(left + 28f, top + 224f, width - 56f, 52f), "Boss Stage"))
            {
                SceneManager.LoadScene(MireColossusBossSceneName);
            }

            GUI.matrix = previousMatrix;
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
    }
}
