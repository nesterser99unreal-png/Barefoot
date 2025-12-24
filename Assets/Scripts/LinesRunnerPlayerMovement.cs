using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using static UnityEngine.InputSystem.InputAction;

public class LinesRunnerPlayerMovement : MonoBehaviour
{
    [SerializeField] private SplineAnimate _splineAnimate;
    [SerializeField] private List<SplineContainer> _paths;
    [SerializeField] private int _defaultPathIndex;

    private int _currentPathIndex;

    private void Awake()
    {
        if (_defaultPathIndex < 0 || _defaultPathIndex >= _paths.Count)
        {
            Debug.LogError($"Default path index {_defaultPathIndex} is out of bounds of paths list size {_paths.Count}");
            return;
        }
        
        _currentPathIndex = _defaultPathIndex;
        _splineAnimate.Container = _paths[_currentPathIndex];
    }

    public void TurnLeft(CallbackContext context)
    {
        if (_currentPathIndex <= 0)
            return;
        
        _currentPathIndex--;
        _splineAnimate.Container = _paths[_currentPathIndex];
    }
    
    public void TurnRight(CallbackContext context)
    {
        if (_currentPathIndex >= _paths.Count - 1)
            return;
        
        _currentPathIndex++;
        _splineAnimate.Container = _paths[_currentPathIndex];
    }

    public void Jump(CallbackContext context) => 
        Debug.Log("Jumped");
}