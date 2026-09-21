public static class AnimationClock
{
    public static long Now => Environment.TickCount64;

    public static float Phase(long now, int periodMs, long seed = 0)
    {
        if (periodMs <= 0)
            return 0f;

        long shifted = now + seed;
        long remainder = shifted % periodMs;
        if (remainder < 0)
            remainder += periodMs;

        return remainder / (float)periodMs;
    }

    public static float Sine(long now, int periodMs, long seed = 0)
    {
        return MathF.Sin(Phase(now, periodMs, seed) * MathF.PI * 2f);
    }

    public static float PingPong(long now, int periodMs, long seed = 0)
    {
        return 0.5f + 0.5f * Sine(now, periodMs, seed);
    }

    public static float EaseOutCubic(float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        float inverse = 1f - t;
        return 1f - inverse * inverse * inverse;
    }

    public static float EaseInOutSine(float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return 0.5f - 0.5f * MathF.Cos(MathF.PI * t);
    }

    public static float Lerp(float a, float b, float t) =>
        a + (b - a) * Math.Clamp(t, 0f, 1f);

    public static int Lerp(int a, int b, float t) =>
        (int)MathF.Round(Lerp((float)a, (float)b, t));

    public static float AttackProgress(long now, long startedAt, int durationMs) =>
        durationMs <= 0
            ? 1f
            : Math.Clamp((now - startedAt) / (float)durationMs, 0f, 1f);

    public static float Decay(long now, long startedAt, int durationMs) =>
        durationMs <= 0
            ? 0f
            : 1f - AttackProgress(now, startedAt, durationMs);

    public static bool IsComplete(long now, long startedAt, int durationMs) =>
        now - startedAt >= durationMs;
}
