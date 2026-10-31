using System;

public enum PlayerHuntPhase { Seeking, Tracking, GracePeriod, Investigating }
public enum PlayerHuntStyle { Stalker = 0, Tracker = 1 }

// Position is generic so the pursuit lifecycle can be tested without a Unity scene.
public sealed class PlayerHuntMemory<TPosition>
{
    public PlayerHuntPhase Phase { get; private set; }
    public TPosition Destination { get; private set; }
    public bool HasDestination => Phase != PlayerHuntPhase.Seeking;
    float loseAt;
    float investigateUntil;

    public void Update(bool validVictim, bool detected, TPosition position, float now, float graceDuration, float investigationDuration,
        PlayerHuntStyle style = PlayerHuntStyle.Stalker)
    {
        if (!validVictim) { Reset(); return; }
        if (detected || (style == PlayerHuntStyle.Tracker && HasDestination))
        {
            Phase = PlayerHuntPhase.Tracking;
            Destination = position;
            return;
        }
        if (Phase == PlayerHuntPhase.Seeking) return;
        if (Phase == PlayerHuntPhase.Tracking)
        {
            Phase = PlayerHuntPhase.GracePeriod;
            loseAt = now + Math.Max(0f, graceDuration);
        }
        if (Phase == PlayerHuntPhase.GracePeriod)
        {
            Destination = position;
            if (now >= loseAt)
            {
                Phase = PlayerHuntPhase.Investigating;
                investigateUntil = now + Math.Max(0f, investigationDuration);
            }
        }
        else if (Phase == PlayerHuntPhase.Investigating && now >= investigateUntil) Reset();
    }

    public void CompleteInvestigation()
    {
        if (Phase == PlayerHuntPhase.Investigating) Reset();
    }

    public void Reset()
    {
        Phase = PlayerHuntPhase.Seeking;
        Destination = default;
        loseAt = investigateUntil = 0f;
    }
}
