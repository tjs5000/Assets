using UnityEngine;
using PlexiPark.Core.SharedEnums;

[CreateAssetMenu(fileName = "NewNeedData", menuName = "PlexiPark/Visitor/Need Data")]
public class NeedData : ScriptableObject
{
    [Tooltip("Which need this represents")]
    public NeedType needType;

    [Tooltip("How much this need decays per second")]
    public float decayRate = 0.01f;

    [Tooltip("How much this need is filled when using a facility")]
    public float fulfillAmount = 0.5f;
}
