// Assets/UI/Views/ModalScrollController.cs

using UnityEngine;

public class ModalScrollController : MonoBehaviour
{
    public static ModalScrollController Instance { get; private set; }
    void Awake() { Instance = this; }
    public void OnScroll(float deltaY) { /* ... */ }
}

