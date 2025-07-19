// Assets/Systems/Input/InputModeRouter.cs
using UnityEngine;
using System;
using PlexiPark.Systems.Input.Interfaces;

namespace PlexiPark.Systems.Input
{
    public class InputModeRouter : MonoBehaviour, IInputRouter
    {
        public static InputModeRouter I { get; private set; }

        public event Action<Vector2> OnPointerMoved;
        public event Action<Vector2> OnPointerDown;
        public event Action<Vector2> OnPointerUp;

        private bool isDragging = false;

        private void Awake()
        {
            if (I != null && I != this)
            {
                Destroy(gameObject);
                return;
            }
            I = this;
        }

        private void Update()
        {
#if UNITY_EDITOR
            HandleMouse();
#endif
            HandleTouch();
        }

        private void HandleMouse()
        {
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                isDragging = true;
                OnPointerDown?.Invoke(UnityEngine.Input.mousePosition);
            }
            else if (UnityEngine.Input.GetMouseButtonUp(0))
            {
                isDragging = false;
                OnPointerUp?.Invoke(UnityEngine.Input.mousePosition);
            }

            if (isDragging)
            {
                OnPointerMoved?.Invoke(UnityEngine.Input.mousePosition);
            }
        }

        private void HandleTouch()
        {
            if (UnityEngine.Input.touchCount == 1)
            {
                Touch t = UnityEngine.Input.GetTouch(0);

                switch (t.phase)
                {
                    case TouchPhase.Began:
                        isDragging = true;
                        OnPointerDown?.Invoke(t.position);
                        break;
                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        if (isDragging)
                            OnPointerMoved?.Invoke(t.position);
                        break;
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        isDragging = false;
                        OnPointerUp?.Invoke(t.position);
                        break;
                }
            }
        }
    }
}
