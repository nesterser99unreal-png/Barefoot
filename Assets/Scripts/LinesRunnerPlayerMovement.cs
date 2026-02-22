using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Splines;
using VContainer;

public class LinesRunnerPlayerMovement : MonoBehaviour
{
    [Header("Preset")]
    [SerializeField] private LinesRunnerMovementInitializationPreset _startPreset;
    
    [Header("Spline")]
    [SerializeField] private SplineContainer _defaultSplineContainer;
    
    private LinesRunnerMovementHandler _movementHandler;

    [Inject]
    public void Initialize(LinesRunnerMovementHandler movementHandler)
    {
        _movementHandler = movementHandler;
        _movementHandler.Initialize(_startPreset, transform, _defaultSplineContainer);
    }

    public void TurnLeft(InputAction.CallbackContext context) => 
        _movementHandler.TryStartLateralJump(-1);

    public void TurnRight(InputAction.CallbackContext context) => 
        _movementHandler.TryStartLateralJump(1);

    public void Jump(InputAction.CallbackContext context) => 
        _movementHandler.TryStartJump();

    private void Update() => 
        _movementHandler.OnUpdate();
}