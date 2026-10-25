using System;
using System.Collections.Generic;
using UnityEngine;

public partial class Game
{
    [Header("Composição e histórico das noites")]
    public NightDirectorData nightDirector;
    public int PlannedNightEnemies { get; private set; }
    public int SpawnedThisNight { get; private set; }
    public int CurrentNightMinimum { get; private set; }
    NightPopulationHistory nightPopulation;
    System.Random compositionRandom;

    sealed class NightEnemySpec
    {
        public EnemyArchetypeData archetype;
        public EnemyType type;
        public Personality personality;
        public float bravery;
        public EnemyTemperament temperament;
        public EnemySocialBehavior behavior;
    }
    sealed class NightGroupPlan
    {
        public NightEnemySpec[] members;
        public GroupTactic tactic;
        public bool fixedOpening;
        public string name;
    }
    sealed class NightPlan
    {
        public int waves, groupsPerWave, total;
        public NightGroupPlan[] groups;
    }

    NightPlan PlanNight(int number)
    {
        if (nightDirector==null) nightDirector=Resources.Load<NightDirectorData>("NightDirector/DefaultNightDirector");
        if (nightDirector==null) throw new InvalidOperationException("Configure um NightDirectorData no Game.");
        int openingPerGroup=checked(Mathf.Max(1,baseCommonEnemies+commonsPerRound)+(1%Mathf.Max(1,tankEveryRounds)==0?1:0));
        int openingWaves=VillageProgressionRules.WavesForNight(1,initialWaves,nightsPerExtraWave,maxWaves);
        int openingTotal=checked(openingWaves*Mathf.Max(1,groupsPerWave)*openingPerGroup);
        if (nightPopulation==null) nightPopulation=new NightPopulationHistory(openingTotal);
        if (compositionRandom==null) compositionRandom=new System.Random(UnityEngine.Random.Range(1,int.MaxValue));
        var random=number==1?new System.Random(nightDirector.firstNightSeed):compositionRandom;
        int minimum=nightPopulation.MinimumFor(number);
        int total=number==1?openingTotal:NightPopulationHistory.RollCount(minimum,nightDirector.minimumExtraEnemies,
            nightDirector.minimumIncreasePercent,nightDirector.maximumIncreasePercent,nightDirector.extraEnemyBatchSize,
            nightDirector.repeatMinimumChance,random);
        int waves=Mathf.Min(total,VillageProgressionRules.WavesForNight(number,initialWaves,nightsPerExtraWave,maxWaves));
        int perWave=Mathf.Min(Mathf.Max(1,groupsPerWave),Mathf.Max(1,total/waves));
        var counts=NightPopulationHistory.Distribute(total,waves*perWave);
        var plan=new NightPlan { waves=waves,groupsPerWave=perWave,total=total,groups=new NightGroupPlan[counts.Length] };
        for(int group=0;group<counts.Length;group++)
        {
            var profile=number==1?null:PickComposition(number,counts[group],total/(float)minimum,random);
            var archetypes=new List<EnemyArchetypeData>();
            if(profile!=null)
            {
                foreach(var slot in profile.members)
                    for(int i=0;i<Mathf.Max(0,slot.minimumPerGroup);i++) archetypes.Add(slot.archetype);
                while(archetypes.Count<counts[group]) archetypes.Add(PickArchetype(profile,random));
            }
            var spec=new NightGroupPlan { members=new NightEnemySpec[counts[group]],fixedOpening=number==1,
                name=profile!=null?profile.displayName:"Grupo comum",
                tactic=profile!=null&&profile.forceTactic?profile.tactic:
                    random.NextDouble()<tacticalGroupChance?GroupTactic.Tactical:GroupTactic.Aggressive };
            for(int i=0;i<spec.members.Length;i++)
            {
                var archetype=profile!=null?archetypes[i]:null;
                var type=archetype!=null?archetype.runtimeType:
                    number%Mathf.Max(1,tankEveryRounds)==0&&i==spec.members.Length-1?EnemyType.Tank:EnemyType.Common;
                double braveryRoll=random.NextDouble();
                float bravery=type==EnemyType.Tank?1f:braveryRoll<0.4?1f:braveryRoll<0.75?0.5f:0.2f;
                if(type!=EnemyType.Tank&&bravery>=0.99f&&varyFanaticBraveryForTesting) bravery=1f+(float)random.NextDouble()*0.2f;
                spec.members[i]=new NightEnemySpec { archetype=archetype,type=type,bravery=bravery,
                    personality=type==EnemyType.Tank?Personality.Objective:profile!=null&&profile.uniformPersonality?profile.personality:
                        random.NextDouble()<Mathf.Min(hunterBaseChance+hunterChancePerRound*number,hunterChanceCap)?Personality.Hunter:Personality.Objective,
                    temperament=number==1?EnemyTemperament.Disciplined:profile!=null&&profile.uniformTemperament?profile.temperament:
                        type==EnemyType.Tank?EnemyTemperament.Disciplined:(EnemyTemperament)random.Next(4),
                    behavior=number==1?EnemySocialBehavior.Proactive:profile!=null&&profile.uniformBehavior?profile.behavior:
                        type==EnemyType.Tank?EnemySocialBehavior.Proactive:(EnemySocialBehavior)random.Next(3) };
            }
            plan.groups[group]=spec;
        }
        CurrentNightMinimum=minimum;
        return plan;
    }

    EnemyGroupCompositionData PickComposition(int night,int groupCount,float ratio,System.Random random)
    {
        var candidates=new List<EnemyGroupCompositionData>();
        float totalWeight=0f;
        foreach(var profile in nightDirector.groupCompositions??new EnemyGroupCompositionData[0])
        {
            if(profile==null||night<Mathf.Max(2,profile.firstNight)||profile.selectionWeight<=0f||
                ratio<profile.minimumPopulationRatio||ratio>profile.maximumPopulationRatio||profile.members==null) continue;
            long required=0; bool valid=profile.members.Length>0; float memberWeight=0f;
            foreach(var slot in profile.members)
            {
                if(slot==null||slot.archetype==null||slot.archetype.balance==null) { valid=false; break; }
                required+=Mathf.Max(0,slot.minimumPerGroup); memberWeight+=Mathf.Max(0f,slot.weight);
            }
            if(!valid||required>groupCount||(required<groupCount&&memberWeight<=0f)) continue;
            totalWeight+=profile.selectionWeight; candidates.Add(profile);
        }
        if(candidates.Count==0) return null;
        double roll=random.NextDouble()*totalWeight;
        foreach(var candidate in candidates) { roll-=candidate.selectionWeight; if(roll<0) return candidate; }
        return candidates[candidates.Count-1];
    }

    static EnemyArchetypeData PickArchetype(EnemyGroupCompositionData profile,System.Random random)
    {
        float total=0f;
        foreach(var slot in profile.members) total+=Mathf.Max(0f,slot.weight);
        double roll=random.NextDouble()*total;
        EnemyArchetypeData last=null;
        foreach(var slot in profile.members)
        {
            if(slot.weight<=0f) continue;
            last=slot.archetype; roll-=slot.weight; if(roll<0) return last;
        }
        return last;
    }
}
