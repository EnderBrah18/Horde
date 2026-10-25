using System;
using System.Collections.Generic;

// Per-run history. A block's floor changes only after five whole nights.
public sealed class NightPopulationHistory
{
    readonly int firstNightCount;
    readonly Dictionary<int,int> peaks = new Dictionary<int,int>();
    readonly HashSet<int> recorded = new HashSet<int>();
    public const int BlockLength = 5;
    public NightPopulationHistory(int firstNightCount) { this.firstNightCount = Math.Max(1,firstNightCount); }

    public int MinimumFor(int night)
    {
        int minimum = firstNightCount;
        int block = (Math.Max(1,night)-1)/BlockLength;
        for (int i=0;i<block;i++) if (peaks.TryGetValue(i,out int peak)) minimum=Math.Max(minimum,peak);
        return minimum;
    }

    public bool RecordCompleted(int night,int actualCount)
    {
        if (night<1 || actualCount<MinimumFor(night) || !recorded.Add(night)) return false;
        int block=(night-1)/BlockLength;
        peaks.TryGetValue(block,out int peak);
        peaks[block]=Math.Max(peak,actualCount);
        return true;
    }

    public int PeakForBlock(int night)
    {
        int block=(Math.Max(1,night)-1)/BlockLength;
        return peaks.TryGetValue(block,out int peak) ? peak : 0;
    }

    public static int RollCount(int minimum,int minimumExtra,int minimumPercent,int maximumPercent,int batchSize,
        double repeatChance,Random random)
    {
        minimum=Math.Max(1,minimum); batchSize=Math.Max(1,batchSize);
        if (random.NextDouble()<Math.Max(0,Math.Min(1,repeatChance))) return minimum;
        long jump=Math.Max(1,Math.Max(minimumExtra,((long)minimum*Math.Max(0,minimumPercent)+99)/100));
        long firstBatch=(jump+batchSize-1)/batchSize;
        long lastBatch=Math.Max(firstBatch,(long)minimum*Math.Max(0,maximumPercent)/100/batchSize);
        lastBatch=Math.Min(lastBatch,((long)int.MaxValue-minimum)/batchSize);
        if (lastBatch<firstBatch) return minimum;
        long batch=firstBatch+(long)(random.NextDouble()*(lastBatch-firstBatch+1));
        return (int)(minimum+batch*batchSize);
    }

    public static int[] Distribute(int total,int groups)
    {
        if (total<1 || groups<1 || groups>total) throw new ArgumentOutOfRangeException();
        var counts=new int[groups];
        for (int i=0;i<groups;i++) counts[i]=total/groups+(i<total%groups?1:0);
        return counts;
    }
}
