using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections;
using TMPro;

using PlexiPark.Systems.CameraControl;

namespace PlexiPark.Systems.CameraControl
{
    /// <summary>
    /// Displays the camera's world position and zoom distance on a UI Text.
    /// </summary>
    public class CameraDebugHUD : MonoBehaviour
    {
        [Tooltip("Drag in a UI Text component here.")]
        public TextMeshProUGUI debugText;

        void Update()
        {
            if (CameraController.Instance == null || debugText == null) return;

            // Read camera position
            Vector3 pos = CameraController.Instance.MainCamera.transform.position;
            // Read zoom distance
            float zoom = CameraController.Instance.ZoomDistance;

            debugText.text =
                $"Cam Pos: X={pos.x:F1} Y={pos.y:F1} Z={pos.z:F1}\n" +
                $"Zoom Dist: {zoom:F1}";
        }
    }
}
