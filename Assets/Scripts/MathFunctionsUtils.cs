using Unity.Mathematics;
using UnityEngine;

public static class MathFunctionsUtils
{
    public static Vector3 CalculateMotionWithConstantAcceleration(float duration, float gravity, Vector3 fromPosition, Vector3 toPosition)
    {
        var vectorGravity = Vector3.down * gravity;
        var delta = toPosition - fromPosition;
        return (delta - 0.5f * vectorGravity * duration.Square()) / duration;
    }

    public static float Square(this float value) => 
        value * value;

    public static void NormalizeHorizontalDirection(this Vector3 vector)
    {
        vector.y = 0f;
        vector.Normalize();
    }
    
    public static Vector3 NormalizeHorizontalDirection(this float3 vector)
    {
        Vector3 vectorVector = vector;
        vectorVector.y = 0f;
        vectorVector.Normalize();
        return vectorVector;
    }
}