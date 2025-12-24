public static class MathFunctionsUtils
{
    public static float GetParabolaHeight(float progress, float maxHeight) => 
        4f * maxHeight * progress * (1f - progress);
}