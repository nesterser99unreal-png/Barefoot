using UnityEngine;

[CreateAssetMenu(menuName = "Settings/LinesRunnerMovementInitializationPreset", fileName = "LinesRunnerMovementInitializationPreset")]
public class LinesRunnerMovementInitializationPreset : ScriptableObject
{
    [Header("Movement")]
    [SerializeField] private float _speed = 5f;

    [Header("Jump")]
    [SerializeField] private float _jumpPower = 6f;
    [SerializeField] private float _gravity = 20f;
    [SerializeField] private float _landingRadius = 0.8f;
    [SerializeField] private int _landingSampleCount = 40;

    [Header("Lateral jump")]
    [SerializeField] private float _lateralJumpMaxDistance = 6f;
    [SerializeField] private float _lateralJumpDuration = 0.4f;

    public float Speed => _speed;
    
    public float JumpPower => _jumpPower;
    public float Gravity => _gravity;
    public float LandingRadius => _landingRadius;
    public int LandingSampleCount => _landingSampleCount;
    
    public float LateralJumpMaxDistance => _lateralJumpMaxDistance;
    public float LateralJumpDuration => _lateralJumpDuration;
}