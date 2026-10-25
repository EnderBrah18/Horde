using System;

// Partitions an already chosen population. Never creates or removes enemies.
public static class NightDeploymentRules
{
    public static int[][] Plan(int total,int waves,int nominalGroups,int smallGroupSize,int night,
        NightDirectorData settings,Random random)
    {
        if(total<1) throw new ArgumentOutOfRangeException(nameof(total));
        waves=Math.Max(1,Math.Min(total,waves));
        nominalGroups=Math.Max(1,nominalGroups);
        smallGroupSize=Math.Max(1,smallGroupSize);
        if(night>1 && random.NextDouble()<settings.singleAssaultChance)
            return new[]{new[]{total}};
        int[] waveTotals=night>1 && random.NextDouble()<settings.concentratedWaveChance
            ? Concentrate(total,waves,settings,random) : NightPopulationHistory.Distribute(total,waves);
        var result=new int[waves][];
        for(int wave=0;wave<waves;wave++)
        {
            int count=waveTotals[wave];
            if(night==1)
            { result[wave]=NightPopulationHistory.Distribute(count,Math.Min(count,nominalGroups)); continue; }
            double dispersed=Math.Max(0f,settings.dispersedGroupWeight);
            double balanced=Math.Max(0f,settings.balancedGroupWeight);
            double army=Math.Max(0f,settings.armyGroupWeight)+Math.Max(0,night-2)*(double)Math.Max(0f,settings.armyWeightPerNight);
            double roll=random.NextDouble()*(dispersed+balanced+army);
            if(roll<dispersed)
            {
                int groups=1+(count-1)/smallGroupSize;
                result[wave]=NightPopulationHistory.Distribute(count,groups);
            }
            else if(roll<dispersed+balanced)
                result[wave]=NightPopulationHistory.Distribute(count,Math.Min(count,nominalGroups));
            else
            {
                // A single army or a large main force with smaller supporting groups.
                int maximumGroups=(int)Math.Min(count,(long)nominalGroups+1);
                int groups=1+(int)(random.NextDouble()*maximumGroups);
                result[wave]=Concentrate(count,groups,settings,random);
            }
        }
        return result;
    }

    static int[] Concentrate(int total,int parts,NightDirectorData settings,Random random)
    {
        if(parts==1) return new[]{total};
        double low=Math.Max(0.5,Math.Min(1,settings.minimumDominantShare));
        double high=Math.Max(low,Math.Min(1,settings.maximumDominantShare));
        double share=low+random.NextDouble()*(high-low);
        int main=Math.Max(1,Math.Min(total-parts+1,(int)Math.Round(total*share)));
        var rest=NightPopulationHistory.Distribute(total-main,parts-1);
        var result=new int[parts];
        int mainIndex=random.Next(parts),next=0;
        for(int i=0;i<parts;i++)result[i]=i==mainIndex?main:rest[next++];
        return result;
    }
}
