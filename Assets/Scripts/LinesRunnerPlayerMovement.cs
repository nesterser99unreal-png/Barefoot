using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Splines;
using static UnityEngine.InputSystem.InputAction;

public class LinesRunnerPlayerMovement : MonoBehaviour
{
    [SerializeField] private SplineAnimate _splineAnimate;
    [SerializeField] private List<LineRunnerPathData> _paths;
    [SerializeField] private int _defaultPathIndex;
    [SerializeField] private Animator _animator;
    
    [SerializeField] private AnimationClip _jumpAnimationClip;
    [SerializeField] private float _jumpHeight = 1.5f;

    private readonly int _jumpHash = Animator.StringToHash("Jump");
    
    private int _currentPathIndex;
    private bool _isMoving;
    
    private Tweener _moveTweener;

    private void Awake()
    {
        if (_defaultPathIndex < 0 || _defaultPathIndex >= _paths.Count)
        {
            Debug.LogError($"Default path index {_defaultPathIndex} is out of bounds of paths list size {_paths.Count}");
            return;
        }
        
        _currentPathIndex = _defaultPathIndex;
        if (_paths[_currentPathIndex] != null && _paths[_currentPathIndex].SplineContainer != null)
            _splineAnimate.Container = _paths[_currentPathIndex].SplineContainer;
    }

    public void TurnLeft(CallbackContext context)
    {
        if (_isMoving || _currentPathIndex <= 0)
            return;
        
        TryTurnToOtherPath(_currentPathIndex - 1);
    }
    
    public void TurnRight(CallbackContext context)
    {
        if (_isMoving || _currentPathIndex >= _paths.Count - 1)
            return;
        
        TryTurnToOtherPath(_currentPathIndex + 1);
    }

    public void Jump(CallbackContext context) => 
        Debug.Log("Jumped");

    private void TryTurnToOtherPath(int targetPathIndex)
    {
        TryKillJumpTweener();
        _isMoving = true;
        
        if (_paths[_currentPathIndex] == null || _paths[_currentPathIndex].SplineAnimate == null ||
            _paths[targetPathIndex] == null || _paths[targetPathIndex].SplineAnimate == null)
        {
            Debug.LogError("Path data or SplineAnimate is null!");
            _isMoving = false;
            return;
        }

        var targetLineTransform = _paths[targetPathIndex].SplineAnimate.transform;

        var jumpDuration = _jumpAnimationClip.length;
        var startPosition = transform.position;

        _animator.SetTrigger(_jumpHash);
        _moveTweener = DOTween.To(
            () => 0f,
            progress =>
            {
                var targetLinePosition = targetLineTransform.position;
                
                var horizontalStart = new Vector3(startPosition.x, 0f, startPosition.z);
                var horizontalEnd = new Vector3(targetLinePosition.x, 0f, targetLinePosition.z);
                var horizontalPosition = Vector3.Lerp(horizontalStart, horizontalEnd, progress);
                
                var parabolaHeight = MathFunctionsUtils.GetParabolaHeight(progress, _jumpHeight);
                var currentHeight = Mathf.Lerp(startPosition.y, targetLinePosition.y, progress);
                var finalYPosition = currentHeight + parabolaHeight;
                
                var finalPosition = new Vector3(horizontalPosition.x, finalYPosition, horizontalPosition.z);
                transform.position = finalPosition;
            },
            1f,
            jumpDuration
        )
        .SetEase(Ease.InOutQuad)
        .OnComplete(() => OnMoveCompleted(targetLineTransform.position, targetPathIndex));
    }

    private void OnMoveCompleted(Vector3 finalTargetPosition, int targetPathIndex)
    {
        TryKillJumpTweener();
        transform.position = finalTargetPosition;
        
        _currentPathIndex = targetPathIndex;
        if (_paths[_currentPathIndex] != null && _paths[_currentPathIndex].SplineContainer != null)
            _splineAnimate.Container = _paths[_currentPathIndex].SplineContainer;

        _isMoving = false;
    
        
    }

    private void TryKillJumpTweener()
    {
        if (_moveTweener != null && _moveTweener.IsActive()) 
            _moveTweener.Kill();
    }

    private void OnDestroy() => 
        TryKillJumpTweener();
}