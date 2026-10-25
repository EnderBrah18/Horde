using System.Collections.Generic;

// One allocator per anchor, shared by every source of followers.
public sealed class FormationSlots<T>
{
    readonly Dictionary<T, int> slots = new Dictionary<T, int>();
    readonly HashSet<int> occupied = new HashSet<int>();
    public int Count => slots.Count;

    public int Claim(T follower)
    {
        if (slots.TryGetValue(follower, out var existing)) return existing;
        int slot = 0;
        while (occupied.Contains(slot)) slot++;
        slots.Add(follower, slot);
        occupied.Add(slot);
        return slot;
    }

    public void Release(T follower)
    {
        if (!slots.TryGetValue(follower, out var slot)) return;
        slots.Remove(follower);
        occupied.Remove(slot);
    }

    public static int Row(int slot) => slot / 3;
    public static int Column(int slot) => slot % 3 == 0 ? 0 : slot % 3 == 1 ? -1 : 1;
}
