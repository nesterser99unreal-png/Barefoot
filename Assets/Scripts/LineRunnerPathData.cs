using System;
using UnityEngine;
using UnityEngine.Splines;

[Serializable]
public class LineRunnerPathData
{
    [SerializeField] private SplineContainer _splineContainer;
    [SerializeField] private SplineAnimate _splineAnimate;
        
    public SplineContainer SplineContainer => _splineContainer;
    public SplineAnimate SplineAnimate => _splineAnimate;
}