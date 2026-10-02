using System.Runtime.CompilerServices;

public static class Math_Utility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float FastAbs(float x) => x < 0 ? -x : x;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Remap(float main, float minIn, float maxIn, float minOut, float maxOut)
    {
        if (maxIn - minIn == 0) return (maxOut + minOut) / 2;
        return minOut + (main - minIn) * (maxOut - minOut) / (maxIn - minIn);
    }
}
