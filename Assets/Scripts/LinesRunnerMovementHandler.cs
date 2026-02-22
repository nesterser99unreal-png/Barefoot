using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Splines;

[UsedImplicitly] //Di registered
public class LinesRunnerMovementHandler
{
    private const float NearEndThreshold = 0.95f;
    
    private LinesRunnerMovementInitializationPreset _startPreset;

    private SplineContainer _splineContainer;
    private Transform _movementTransform;

    private bool _isInAir;
    private float _distanceTraveled;

    private float _cachedTotalLength = -1f;
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
    
    public void Initialize(
        LinesRunnerMovementInitializationPreset startPreset, 
        Transform movementTransform,
        SplineContainer defaultSplineContainer)
    {
        _startPreset = startPreset;
        _splineContainer = defaultSplineContainer;
        _movementTransform = movementTransform;
    }

    public void OnUpdate()
    {
        if (_isInAir)
        {
            UpdateJump();
            return;
        }

        UpdateMovementOnSpline();
    }
    
    public void TryStartJump()
    {
        if (!CanJump())
            return;
        
        var normalizedCurrentPosition = Mathf.Clamp01(_distanceTraveled / TotalLength);
        _jumpTakeoffNormalizedPosition = normalizedCurrentPosition;
        _splineContainer.Evaluate(normalizedCurrentPosition, out _, out var tangent, out _);
        
        _jumpVelocity = GetInitialVectorJumpVelocity();
        _isInAir = true;
        
        Vector3 GetInitialVectorJumpVelocity()
        {
            Vector3 vectorTangent = tangent;
            return vectorTangent.normalized * _startPreset.Speed + Vector3.up * _startPreset.JumpPower;
        }
    }
    
    public void TryStartLateralJump(int direction)
    {
        if (!CanJump())
            return;
        
        var normalizedCurrentPosition = Mathf.Clamp01(_distanceTraveled / TotalLength);
        _splineContainer.Evaluate(normalizedCurrentPosition, out var fromPosition, out var tangent, out var up);
        
        var forward = tangent.NormalizeHorizontalDirection();

        var rightDirection = Vector3.Cross(up, tangent).normalized;
        var lateralDirection = (direction > 0 ? 1 : -1) * rightDirection;

        if (TryFindPathInDirection(fromPosition, lateralDirection, out var targetContainer, out var landingNormalizedPosition))
        {
            Vector3 landingPosition = targetContainer.EvaluatePosition(landingNormalizedPosition);
            _lateralJumpTargetContainer = targetContainer;
            _lateralJumpLandingNormalizedPosition = landingNormalizedPosition;
            _lateralJumpStartTime = Time.time;
            
            var requiredVelocity = 
                MathFunctionsUtils.CalculateMotionWithConstantAcceleration(_startPreset.LateralJumpDuration, _startPreset.Gravity, fromPosition, landingPosition);
            var residual = requiredVelocity - forward * _startPreset.Speed;
            
            var lateralSpeed = Vector3.Dot(residual, lateralDirection);
            var verticalSpeed = Vector3.Dot(residual, Vector3.up);
            _jumpVelocity = GetInitialVectorJumpVelocity(lateralSpeed, verticalSpeed);
        }
        else
        {
            _lateralJumpTargetContainer = null;
            _jumpVelocity = GetInitialVectorJumpVelocity(_startPreset.JumpPower, _startPreset.JumpPower);
        }

        _jumpTakeoffNormalizedPosition = normalizedCurrentPosition;
        _isInAir = true;
        
        Vector3 GetInitialVectorJumpVelocity(float lateralSpeed, float verticalSpeed) => 
            forward * _startPreset.Speed + lateralDirection * lateralSpeed + Vector3.up * verticalSpeed;
    }
    
    private bool TryFindPathInDirection(Vector3 fromPosition, Vector3 lateralDirection, out SplineContainer targetContainer, out float landingNormalizedPosition)
    {
        targetContainer = null;
        landingNormalizedPosition = 0f;

        lateralDirection.NormalizeHorizontalDirection();

        var allContainers = Object.FindObjectsByType<SplineContainer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        var maxDistanceSquare = _startPreset.LateralJumpMaxDistance.Square();
        var bestDistanceSquare = float.MaxValue;

        foreach (var container in allContainers)
        {
            if (container == _splineContainer)
                continue;

            for (var i = 0; i <= _startPreset.LandingSampleCount; i++)
            {
                var normalizedPositionOnSpline = i / (float)_startPreset.LandingSampleCount;
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
        _jumpVelocity.y -= _startPreset.Gravity * Time.deltaTime;
        _movementTransform.position += _jumpVelocity * Time.deltaTime;

        if (_lateralJumpTargetContainer != null)
            TryLandOnSplineAfterLateralJump();
        else
            TryLandOnSplineAfterJump();
    }
    
    private void UpdateMovementOnSpline()
    {
        _distanceTraveled += _startPreset.Speed * Time.deltaTime;
        _distanceTraveled = Mathf.Clamp(_distanceTraveled, 0f, TotalLength);

        var normalizedCurrentPosition = _distanceTraveled / TotalLength;
        normalizedCurrentPosition = Mathf.Clamp01(normalizedCurrentPosition);

        _splineContainer.Evaluate(normalizedCurrentPosition, out var position, out var tangent, out var up);

        _movementTransform.position = position;
        _movementTransform.rotation = Quaternion.LookRotation(tangent, up);
    }
    
    private void TryLandOnSplineAfterLateralJump()
    {
        var landingRadiusSquare = _startPreset.LandingRadius.Square();

        Vector3 landingPoint = _lateralJumpTargetContainer.EvaluatePosition(_lateralJumpLandingNormalizedPosition);
        var elapsedTime = Time.time - _lateralJumpStartTime;
        var withinRadius = (_movementTransform.position - landingPoint).sqrMagnitude < landingRadiusSquare;
        var timeReached = elapsedTime >= _startPreset.LateralJumpDuration;

        if (withinRadius || timeReached)
        {
            var bestDistanceSquare = (_movementTransform.position - landingPoint).sqrMagnitude;
            for (var i = 0; i <= _startPreset.LandingSampleCount; i++)
            {
                var normalizedPositionOnSpline = i / (float)_startPreset.LandingSampleCount;
                if (normalizedPositionOnSpline < _lateralJumpLandingNormalizedPosition)
                    continue;

                Vector3 destinationPoint = _lateralJumpTargetContainer.EvaluatePosition(normalizedPositionOnSpline);
                var destinationSquare = (_movementTransform.position - destinationPoint).sqrMagnitude;

                if (destinationSquare < bestDistanceSquare)
                {
                    bestDistanceSquare = destinationSquare;
                    _lateralJumpLandingNormalizedPosition = normalizedPositionOnSpline;
                }
            }
            
            if (_lateralJumpLandingNormalizedPosition >= NearEndThreshold)
            {
                var pathEndPoint = _lateralJumpTargetContainer.EvaluatePosition(1f);
                Vector3 pathEndVector = pathEndPoint;
                _lateralJumpTargetContainer.Evaluate(1f, out _, out var pathEndTangent, out _);
                Vector3 pathEndForward = pathEndTangent;
                pathEndForward.y = 0f;
                if (pathEndForward.sqrMagnitude > 0.001f)
                {
                    pathEndForward.Normalize();
                    var toCharacter = _movementTransform.position - pathEndVector;
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
        var landingRadiusSquare = _startPreset.LandingRadius.Square();
        var allContainers = Object.FindObjectsByType<SplineContainer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (var container in allContainers)
        {
            if (container == null)
                continue;

            var bestDistanceSquare = float.MaxValue;
            var bestNormalizedPositionOnSpline = 0f;

            for (var i = 0; i <= _startPreset.LandingSampleCount; i++)
            {
                var normalizedPositionOnSpline = i / (float)_startPreset.LandingSampleCount;
                Vector3 destinationPoint = container.EvaluatePosition(normalizedPositionOnSpline);
                var distanceSquare = (_movementTransform.position - destinationPoint).sqrMagnitude;
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
                bestNormalizedPositionOnSpline <= _jumpTakeoffNormalizedPosition + _startPreset.LandingRadius;

            return !isCurrentContainer || !isInvalidDistanceForCurrentContainer;
        }
    }

    private bool CanJump() => 
        !_isInAir;
    
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