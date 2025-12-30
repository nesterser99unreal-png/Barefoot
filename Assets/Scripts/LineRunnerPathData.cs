using System;
using UnityEngine;
using UnityEngine.Splines;

[Serializable]
public class LineRunnerPathData
{
    private const float smallOffsetTraveledPassed = 0.1f;
    
    [SerializeField] private SplineContainer _splineContainer;
    [SerializeField] private SplineAnimate _splineAnimate;

    private SplinePath<Spline> _splinePath;
    
    public SplineContainer SplineContainer => _splineContainer;
    public SplineAnimate SplineAnimate => _splineAnimate;

    public int GetCurrentSplineIndex()
    {
        var normalizedTime = _splineAnimate.NormalizedTime;
        var totalLength = GetLenght();
        
        var traveledDistance = normalizedTime * totalLength;
        var accumulatedLength = 0f;
        
        for (var index = 0; index < _splineContainer.Splines.Count; index++)
        {
            var splineLength = _splineContainer.Splines[index].GetLength();
            if (traveledDistance <= accumulatedLength + splineLength - smallOffsetTraveledPassed)
                return index;

            accumulatedLength += splineLength;
        }
        
        return _splineContainer.Splines.Count - 1;
    }

    private float GetLenght()
    {
        _splinePath ??= new SplinePath<Spline>(_splineContainer.Splines);
        return _splinePath.GetLength();
    }
}