using UnityEngine;
using UnityEngine.UI;

   public class GhostContextUI : MonoBehaviour
    {
        public Button commitButton;
        public Button cancelButton;
        public Button rotateButton;

        private Transform target; // the ghost to follow

        public void Initialize(Transform followTarget)
        {
            target = followTarget;
        }

        void Update()
        {
            if (target == null)
            {
                Destroy(gameObject);
                return;
            }

            transform.position = target.position + Vector3.up * 2f; // hover above ghost
            transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward); // face camera
        }
    }
