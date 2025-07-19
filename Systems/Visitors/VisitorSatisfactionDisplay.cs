// Assets/Systems/Visitor/VisitorSatisfactionDisplay.cs
using UnityEngine;
using TMPro;
using PlexiPark.Systems.Visitor;

[RequireComponent(typeof(VisitorMoodManager))]
public class VisitorSatisfactionDisplay : MonoBehaviour
{
    [SerializeField] private VisitorMoodManager _moodManager;
    [SerializeField] private TextMeshPro _textField;

    void Awake()
    {
        // if you forget to wire it up in the Inspector, grab the component automatically:
        if (_moodManager == null) _moodManager = GetComponent<VisitorMoodManager>();
    }

    void OnEnable()
    {
        if (_moodManager == null || _textField == null)
        {
            Debug.LogWarning("VisitorSatisfactionDisplay: missing reference", this);
            enabled = false;
            return;
        }

        // subscribe & initialize
        _moodManager.OnMetricsUpdated += UpdateText;
        UpdateText(_moodManager.Metrics);
    }

    void OnDisable()
    {
        if (_moodManager != null)
            _moodManager.OnMetricsUpdated -= UpdateText;
    }

    private void UpdateText(VisitorMetrics metrics)
    {
        // format the Overall satisfaction as a percentage
        _textField.text = $"{metrics.OverallSatisfaction * 100f:0}%";
    }
}
