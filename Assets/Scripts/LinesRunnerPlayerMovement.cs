using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Splines;

public class LinesRunnerPlayerMovement : MonoBehaviour
{
    [Header("Spline")]
    [SerializeField] private SplineContainer _splineContainer;

    [Header("Movement")]
    [SerializeField] private float _speed = 5f;

    [Header("Jump over gap")]
    [SerializeField] private float _jumpPower = 6f;
    [SerializeField] private float _gravity = 20f;
    [SerializeField] private float _landingRadius = 0.8f;
    [SerializeField] private int _landingSampleCount = 40;

    private float _distanceTraveled;
    private float _cachedTotalLength = -1f;
    private bool _isInAir;
    private Vector3 _jumpVelocity;
    private float _jumpTakeoffNormalizedPosition;

    private float TotalLength
    {
        get
        {
            if (_splineContainer == null) return 0f;
            if (_cachedTotalLength < 0f)
                _cachedTotalLength = _splineContainer.CalculateLength();
            return _cachedTotalLength;
        }
    }

    public void TurnLeft(InputAction.CallbackContext context)
    {
        
    }

    public void TurnRight(InputAction.CallbackContext context)
    {
        
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (_isInAir)
            return;
        
        StartJump();
    }

    private void Update()
    {
        if (_isInAir)
        {
            UpdateJump();
            return;
        }

        UpdateMovementOnSpline();
    }

    private void StartJump()
    {
        var normalizedCurrentPosition = Mathf.Clamp01(_distanceTraveled / TotalLength);
        _jumpTakeoffNormalizedPosition = normalizedCurrentPosition;
        _splineContainer.Evaluate(normalizedCurrentPosition, out _, out var tangent, out _);

        Vector3 vectorTangent = tangent;
        _jumpVelocity = vectorTangent.normalized * _speed + Vector3.up * _jumpPower;
        _isInAir = true;
    }

    private void UpdateJump()
    {
        _jumpVelocity.y -= _gravity * Time.deltaTime;
        transform.position += _jumpVelocity * Time.deltaTime;

        TryLandOnSpline();
    }

    private void UpdateMovementOnSpline()
    {
        _distanceTraveled += _speed * Time.deltaTime;
        _distanceTraveled = Mathf.Clamp(_distanceTraveled, 0f, TotalLength);

        var normalizedCurrentPosition = _distanceTraveled / TotalLength;
        normalizedCurrentPosition = Mathf.Clamp01(normalizedCurrentPosition);

        _splineContainer.Evaluate(normalizedCurrentPosition, out var position, out var tangent, out var up);

        transform.position = position;
        transform.rotation = Quaternion.LookRotation(tangent, up);
    }

    private void TryLandOnSpline()
    {
        var allContainers = FindObjectsByType<SplineContainer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        var landingRadiusSquare = _landingRadius * _landingRadius;

        foreach (var container in allContainers)
        {
            if (container == null)
                continue;

            var bestDistanceSquare = float.MaxValue;
            var bestNormalizedPositionOnSpline = 0f;

            for (var i = 0; i <= _landingSampleCount; i++)
            {
                var normalizedPositionOnSpline = i / (float)_landingSampleCount;
                Vector3 destinationPoint = container.EvaluatePosition(normalizedPositionOnSpline);
                var distanceSquare = (transform.position - destinationPoint).sqrMagnitude;
                if (distanceSquare < bestDistanceSquare)
                {
                    bestDistanceSquare = distanceSquare;
                    bestNormalizedPositionOnSpline = normalizedPositionOnSpline;
                }
            }

            if (bestDistanceSquare >= landingRadiusSquare)
                continue;
            
            if (!CanLandOnPath(container, bestNormalizedPositionOnSpline)) 
                continue;

            LandOnPath(container, bestNormalizedPositionOnSpline);
            return;
        }
        
        bool CanLandOnPath(SplineContainer container, float bestNormalizedPositionOnSpline)
        {
            var isCurrentContainer = container == _splineContainer;
            var isInvalidDistanceForCurrentContainer =
                bestNormalizedPositionOnSpline <= _jumpTakeoffNormalizedPosition + _landingRadius;
        
            return !isCurrentContainer || !isInvalidDistanceForCurrentContainer;
        }
    }

    private void LandOnPath(SplineContainer container, float normalizedPositionOnSpline)
    {
        _splineContainer = container;
        InvalidateLengthCache();
        
        _distanceTraveled = Mathf.Clamp01(normalizedPositionOnSpline) * TotalLength;
        _isInAir = false;
    }

    private void InvalidateLengthCache() => 
        _cachedTotalLength = -1f;
}