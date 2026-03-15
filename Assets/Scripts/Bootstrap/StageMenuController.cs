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
            var width = 420f;
            var height = 270f;
            var left = (Screen.width - width) * 0.5f;
            var top = (Screen.height - height) * 0.5f;

            GUI.Box(new Rect(left, top, width, height), "Dagon Test Stages");
            GUI.Label(new Rect(left + 24f, top + 36f, width - 48f, 24f), "Choose a scene for focused testing.");
            GUI.Label(new Rect(left + 24f, top + 62f, width - 48f, 60f), "Current content available: main Black Mire run flow and the Mire Colossus boss encounter.");

            if (GUI.Button(new Rect(left + 24f, top + 126f, width - 48f, 40f), "Black Mire Run"))
            {
                SceneManager.LoadScene(BlackMireSceneName);
            }

            if (GUI.Button(new Rect(left + 24f, top + 176f, width - 48f, 40f), "Mire Colossus Boss Test"))
            {
                SceneManager.LoadScene(MireColossusBossSceneName);
            }
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
