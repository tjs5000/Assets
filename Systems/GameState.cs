using UnityEngine;
using System;
using PlexiPark.Core.SharedEnums;
using PlexiPark.Data;

public class GameState : MonoBehaviour
{
    public static GameState I { get; private set; }

    private InputMode mode;
    public InputMode Mode
    {
        get => mode;
        set
        {
            if (mode == value) return;
            mode = value;
            OnModeChanged?.Invoke(mode);
        }
    }

    public event Action<InputMode> OnModeChanged;

    private void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    public void EnterBuildMode()
    {
        Mode = InputMode.Build;
    }

    public void ExitBuildMode()
    {
        Mode = InputMode.Idle; // No need to re-invoke the event
    }
}
