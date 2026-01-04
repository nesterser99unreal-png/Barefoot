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
    [SerializeField] private Rigidbody _rigidbody;
    
    [SerializeField] private AnimationClip _jumpAnimationClip;
    [SerializeField] private float _jumpHeight = 1.5f;

    private readonly int _jumpHash = Animator.StringToHash("Jump");
    
    private int _currentPathIndex;
    private int _currentSplineIndex;
    
    private bool _isMoving;
    private bool _isJumping;
    
    private Tweener _moveTweener;
    private Sequence _jumpSequence;

    private void Awake()
    {
        if (_defaultPathIndex < 0 || _defaultPathIndex >= _paths.Count)
        {
            Debug.LogError($"Default path index {_defaultPathIndex} is out of bounds of paths list size {_paths.Count}");
            return;
        }
        
        _currentPathIndex = _defaultPathIndex;
        _rigidbody.useGravity = false;
        if (_paths[_currentPathIndex] != null && _paths[_currentPathIndex].SplineContainer != null)
            _splineAnimate.Container = _paths[_currentPathIndex].SplineContainer;
    }

    private void Update()
    {
        if (_isJumping || _currentSplineIndex >= _paths[_currentPathIndex].GetCurrentSplineIndex()) 
            return;
        
        _rigidbody.useGravity = true;
        _splineAnimate.enabled = false;
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

    public void Jump(CallbackContext context)
    {
        if (_isJumping || _isMoving)
            return;
        
        _isJumping = true;
        
        var jumpDuration = _jumpAnimationClip.length;
        var endPosition = _paths[_currentPathIndex].GetPositionByTime(jumpDuration, _splineAnimate);
        
        // Кастомная кривая: очень резкое отталкивание, максимально плавный полет, очень резкое приземление
        var jumpCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 4f),       // Очень резкое отталкивание
            new Keyframe(0.02f, 0.02f, 2f, 2f), // Резкий переход
            new Keyframe(0.04f, 0.04f, 1.5f, 1.5f), // Плавный переход
            new Keyframe(0.07f, 0.07f, 1.2f, 1.2f), // Плавный переход
            new Keyframe(0.1f, 0.1f, 1f, 1f),   // Плавный переход после отталкивания
            new Keyframe(0.12f, 0.12f, 0.9f, 0.9f), // Плавный подъем
            new Keyframe(0.15f, 0.15f, 0.8f, 0.8f), // Плавный подъем
            new Keyframe(0.18f, 0.18f, 0.75f, 0.75f), // Плавный подъем
            new Keyframe(0.2f, 0.2f, 0.7f, 0.7f), // Плавный подъем
            new Keyframe(0.23f, 0.23f, 0.65f, 0.65f), // Плавный подъем
            new Keyframe(0.25f, 0.25f, 0.6f, 0.6f), // Плавный подъем
            new Keyframe(0.28f, 0.28f, 0.57f, 0.57f), // Плавный подъем
            new Keyframe(0.3f, 0.3f, 0.55f, 0.55f), // Плавный подъем
            new Keyframe(0.33f, 0.33f, 0.52f, 0.52f), // Плавный подъем
            new Keyframe(0.35f, 0.35f, 0.5f, 0.5f), // Плавный подъем
            new Keyframe(0.38f, 0.38f, 0.47f, 0.47f), // Плавный подъем
            new Keyframe(0.4f, 0.4f, 0.45f, 0.45f), // Максимально плавный подъем
            new Keyframe(0.43f, 0.43f, 0.42f, 0.42f), // Максимально плавный подъем
            new Keyframe(0.45f, 0.45f, 0.4f, 0.4f), // Максимально плавный подъем
            new Keyframe(0.48f, 0.48f, 0.4f, 0.4f), // Максимально плавный подъем
            new Keyframe(0.5f, 0.5f, 0.4f, 0.4f),   // Максимально плавный центр
            new Keyframe(0.52f, 0.52f, 0.4f, 0.4f), // Максимально плавный спуск
            new Keyframe(0.55f, 0.55f, 0.4f, 0.4f), // Максимально плавный спуск
            new Keyframe(0.58f, 0.58f, 0.42f, 0.42f), // Максимально плавный спуск
            new Keyframe(0.6f, 0.6f, 0.45f, 0.45f), // Максимально плавный спуск
            new Keyframe(0.63f, 0.63f, 0.47f, 0.47f), // Плавный спуск
            new Keyframe(0.65f, 0.65f, 0.5f, 0.5f), // Плавный спуск
            new Keyframe(0.68f, 0.68f, 0.52f, 0.52f), // Плавный спуск
            new Keyframe(0.7f, 0.7f, 0.55f, 0.55f), // Плавный спуск
            new Keyframe(0.73f, 0.73f, 0.57f, 0.57f), // Плавный спуск
            new Keyframe(0.75f, 0.75f, 0.6f, 0.6f), // Плавный спуск
            new Keyframe(0.78f, 0.78f, 0.65f, 0.65f), // Плавный спуск
            new Keyframe(0.8f, 0.8f, 0.7f, 0.7f), // Плавный спуск
            new Keyframe(0.83f, 0.83f, 0.75f, 0.75f), // Плавный спуск
            new Keyframe(0.85f, 0.85f, 0.9f, 0.9f), // Плавный спуск (подготовка к резкому финишу)
            new Keyframe(0.88f, 0.88f, 0.95f, 0.95f), // Плавный спуск
            new Keyframe(0.9f, 0.9f, 1f, 1f),   // Начало резкого финиша (симметрично 0.1f, 0.14f)
            new Keyframe(0.93f, 0.93f, 1.2f, 1.2f), // Симметрично 0.07f, 0.1f
            new Keyframe(0.96f, 0.96f, 1.5f, 1.5f), // Симметрично 0.04f, 0.06f
            new Keyframe(0.98f, 0.98f, 2f, 2f), // Симметрично 0.02f, 0.03f
            new Keyframe(1f, 1f, 4f, 0f)        // Очень резкое приземление (симметрично 0f, 0f)
        );
        
        _jumpSequence = transform.DOJump(endPosition, _jumpHeight, 1, jumpDuration)
            .SetEase(jumpCurve)
            .OnComplete(OnJumpCompleted);
    }

    private void TryTurnToOtherPath(int targetPathIndex)
    {
        if (_isJumping || _isMoving)
            return;
        
        TryKillMoveTweener();
        _isMoving = true;
        
        if (_paths[_currentPathIndex] == null || _paths[_currentPathIndex].SplineAnimate == null ||
            _paths[targetPathIndex] == null || _paths[targetPathIndex].SplineAnimate == null)
        {
            Debug.LogError("Path data or SplineAnimate is null!");
            _isMoving = false;
            return;
        }

        var targetLineTransform = _paths[targetPathIndex].SplineAnimate.transform;

        var moveDuration = _jumpAnimationClip.length;
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
            moveDuration
        )
        .SetEase(Ease.InOutQuad)
        .OnComplete(() => OnMoveCompleted(targetLineTransform.position, targetPathIndex));
    }

    private void OnMoveCompleted(Vector3 finalTargetPosition, int targetPathIndex)
    {
        TryKillMoveTweener();
        transform.position = finalTargetPosition;
        
        _currentPathIndex = targetPathIndex;
        _currentSplineIndex = _paths[_currentPathIndex].GetCurrentSplineIndex();
        
        if (_paths[_currentPathIndex] != null && _paths[_currentPathIndex].SplineContainer != null)
            _splineAnimate.Container = _paths[_currentPathIndex].SplineContainer;

        _isMoving = false;
    }

    private void OnJumpCompleted() => 
        _isJumping = false;

    private void TryKillMoveTweener()
    {
        if (_moveTweener != null && _moveTweener.IsActive()) 
            _moveTweener.Kill();
    }

    private void OnDestroy() => 
        TryKillMoveTweener();
}