using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Splines;

public class LinesRunnerPlayerMovement : MonoBehaviour
{
    [Header("Spline")]
    [SerializeField] private SplineContainer _splineContainer;

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

    private float _distanceTraveled;
    private float _cachedTotalLength = -1f;
    private bool _isInAir;
    private Vector3 _jumpVelocity;
    private float _jumpTakeoffNormalizedPosition;
    
    private SplineContainer _lateralJumpTargetContainer;
    private float _lateralJumpLandingNormalizedPosition;
    private float _lateralJumpStartTime;

    private float TotalLength
    {
        get
        {
            if (_splineContainer == null) 
                return 0f;
            
            if (_cachedTotalLength < 0f)
                _cachedTotalLength = _splineContainer.CalculateLength();
            
            return _cachedTotalLength;
        }
    }

    public void TurnLeft(InputAction.CallbackContext context)
    {
        if (_isInAir)
            return;
        
        StartLateralJump(-1);
    }

    public void TurnRight(InputAction.CallbackContext context)
    {
        if ( _isInAir)
            return;
        
        StartLateralJump(1);
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
    
    private void StartLateralJump(int direction)
    {
        var normalizedCurrentPosition = Mathf.Clamp01(_distanceTraveled / TotalLength);
        _splineContainer.Evaluate(normalizedCurrentPosition, out var fromPosition, out var tangent, out var up);

        Vector3 forward = tangent;
        forward.y = 0f;
        forward.Normalize();

        var rightDirection = Vector3.Cross(up, tangent).normalized;
        var lateralDirection = (direction > 0 ? 1 : -1) * rightDirection;

        if (TryFindPathInDirection(fromPosition, lateralDirection, out var targetContainer, out var landingNormalizedPosition))
        {
            Vector3 landingPosition = targetContainer.EvaluatePosition(landingNormalizedPosition);
            _lateralJumpTargetContainer = targetContainer;
            _lateralJumpLandingNormalizedPosition = landingNormalizedPosition;
            _lateralJumpStartTime = Time.time;

            var gravity = Vector3.down * _gravity;
            Vector3 fromPositionVector = fromPosition;
            var delta = landingPosition - fromPositionVector;
            var requiredVelocity = (delta - 0.5f * gravity * _lateralJumpDuration * _lateralJumpDuration) / _lateralJumpDuration;
            var residual = requiredVelocity - forward * _speed;
            var lateralSpeed = Vector3.Dot(residual, lateralDirection);
            var verticalSpeed = Vector3.Dot(residual, Vector3.up);
            _jumpVelocity = forward * _speed + lateralDirection * lateralSpeed + Vector3.up * verticalSpeed;
        }
        else
        {
            _lateralJumpTargetContainer = null;
            _jumpVelocity = forward * _speed + lateralDirection * _jumpPower + Vector3.up * _jumpPower;
        }

        _jumpTakeoffNormalizedPosition = normalizedCurrentPosition;
        _isInAir = true;
    }
    
    private bool TryFindPathInDirection(Vector3 fromPosition, Vector3 lateralDirection, out SplineContainer targetContainer, out float landingNormalizedPosition)
    {
        targetContainer = null;
        landingNormalizedPosition = 0f;
        
        lateralDirection.y = 0f;
        lateralDirection.Normalize();

        var allContainers = FindObjectsByType<SplineContainer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        var maxDistanceSquare = _lateralJumpMaxDistance * _lateralJumpMaxDistance;
        var bestDistanceSquare = float.MaxValue;

        foreach (var container in allContainers)
        {
            if (container == _splineContainer)
                continue;

            for (var i = 0; i <= _landingSampleCount; i++)
            {
                var normalizedPositionOnSpline = i / (float)_landingSampleCount;
                Vector3 destinationPoint = container.EvaluatePosition(normalizedPositionOnSpline);
                var destinationVector = destinationPoint - fromPosition;
                destinationVector.y = 0f;

                var lateralAmount = Vector3.Dot(destinationVector, lateralDirection);
                if (lateralAmount <= 0f)
                    continue;

                var distanceSquare = destinationVector.sqrMagnitude;
                if (distanceSquare > maxDistanceSquare || distanceSquare >= bestDistanceSquare)
                    continue;

                bestDistanceSquare = distanceSquare;
                targetContainer = container;
                landingNormalizedPosition = normalizedPositionOnSpline;
            }
        }

        return targetContainer != null;
    }

    private void UpdateJump()
    {
        _jumpVelocity.y -= _gravity * Time.deltaTime;
        transform.position += _jumpVelocity * Time.deltaTime;

        if (_lateralJumpTargetContainer != null)
            TryLandOnSplineAfterLateralJump();
        else
            TryLandOnSplineAfterJump();
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

    private void TryLandOnSplineAfterLateralJump()
    {
        var landingRadiusSquare = _landingRadius * _landingRadius;

        Vector3 landingPoint = _lateralJumpTargetContainer.EvaluatePosition(_lateralJumpLandingNormalizedPosition);
        var elapsedTime = Time.time - _lateralJumpStartTime;
        var withinRadius = (transform.position - landingPoint).sqrMagnitude < landingRadiusSquare;
        var timeReached = elapsedTime >= _lateralJumpDuration;

        if (withinRadius || timeReached)
        {
            var bestDistanceSquare = (transform.position - landingPoint).sqrMagnitude;
            for (var i = 0; i <= _landingSampleCount; i++)
            {
                var normalizedPositionOnSpline = i / (float)_landingSampleCount;
                if (normalizedPositionOnSpline < _lateralJumpLandingNormalizedPosition)
                    continue;

                Vector3 destinationPoint = _lateralJumpTargetContainer.EvaluatePosition(normalizedPositionOnSpline);
                var destinationSquare = (transform.position - destinationPoint).sqrMagnitude;

                if (destinationSquare < bestDistanceSquare)
                {
                    bestDistanceSquare = destinationSquare;
                    _lateralJumpLandingNormalizedPosition = normalizedPositionOnSpline;
                }
            }

            const float nearEndThreshold = 0.95f;
            if (_lateralJumpLandingNormalizedPosition >= nearEndThreshold)
            {
                var pathEndPoint = _lateralJumpTargetContainer.EvaluatePosition(1f);
                Vector3 pathEndVector = pathEndPoint;
                _lateralJumpTargetContainer.Evaluate(1f, out _, out var pathEndTangent, out _);
                Vector3 pathEndForward = pathEndTangent;
                pathEndForward.y = 0f;
                if (pathEndForward.sqrMagnitude > 0.001f)
                {
                    pathEndForward.Normalize();
                    var toCharacter = transform.position - pathEndVector;
                    toCharacter.y = 0f;
                    if (Vector3.Dot(toCharacter, pathEndForward) > 0f)
                    {
                        _lateralJumpTargetContainer = null;
                        return;
                    }
                }
            }

            LandOnPath(_lateralJumpTargetContainer, _lateralJumpLandingNormalizedPosition);
            _lateralJumpTargetContainer = null;
        }
    }

    private void TryLandOnSplineAfterJump()
    {
        var landingRadiusSquare = _landingRadius * _landingRadius;
        var allContainers = FindObjectsByType<SplineContainer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

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