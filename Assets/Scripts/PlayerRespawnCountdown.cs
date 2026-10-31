using System;

public sealed class PlayerRespawnCountdown
{
    public bool Running { get; private set; }
    public float Remaining { get; private set; }
    public bool Start(float duration)
    {
        if (Running) return false;
        Running = true;
        Remaining = Math.Max(0f, duration);
        return true;
    }
    public bool Tick(float deltaTime, bool coreDestroyed)
    {
        if (!Running) return false;
        if (coreDestroyed) { Cancel(); return false; }
        Remaining = Math.Max(0f, Remaining - Math.Max(0f, deltaTime));
        if (Remaining > 0f) return false;
        Running = false;
        return true;
    }
    public void Cancel() { Running = false; Remaining = 0f; }
}
