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
        public EnemyBalanceSettings balance;
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
        public int[] waveGroupCounts;
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
        var deployment=NightDeploymentRules.Plan(total,waves,groupsPerWave,openingPerGroup,number,nightDirector,random);
        waves=deployment.Length;
        var counts=new List<int>();
        var waveGroupCounts=new int[waves];
        for(int wave=0;wave<waves;wave++)
        { counts.AddRange(deployment[wave]); waveGroupCounts[wave]=deployment[wave].Length; }
        var plan=new NightPlan { waves=waves,groupsPerWave=waveGroupCounts[0],waveGroupCounts=waveGroupCounts,
            total=total,groups=new NightGroupPlan[counts.Count] };
        for(int group=0;group<counts.Count;group++)
        {
            var profiles=new List<EnemyGroupCompositionData>();
            var archetypes=new List<EnemyArchetypeData>();
            var names=new List<string>();
            var tactic=random.NextDouble()<tacticalGroupChance?GroupTactic.Tactical:GroupTactic.Aggressive;
            bool tacticChosen=false;
            while(number>1 && archetypes.Count<counts[group])
            {
                int remaining=counts[group]-archetypes.Count;
                var selected=PickComposition(number,remaining,total/(float)minimum,random);
                if(selected==null) break;
                int count=selected.spawnCount>0?Mathf.Min(selected.spawnCount,remaining):remaining;
                int start=archetypes.Count;
                foreach(var slot in selected.members)
                    for(int i=0;i<Mathf.Max(0,slot.minimumPerGroup);i++) archetypes.Add(slot.archetype);
                while(archetypes.Count<start+count) archetypes.Add(PickArchetype(selected,random));
                while(profiles.Count<archetypes.Count) profiles.Add(selected);
                names.Add(selected.displayName);
                // A physical group has one tactic; the first explicit instruction wins.
                if(!tacticChosen && selected.forceTactic) { tactic=selected.tactic; tacticChosen=true; }
            }
            while(archetypes.Count<counts[group]) { archetypes.Add(null); profiles.Add(null); }
            var spec=new NightGroupPlan { members=new NightEnemySpec[counts[group]],fixedOpening=number==1,
                name=names.Count>0?string.Join(" + ",names):"Grupo comum",tactic=tactic };
            for(int i=0;i<spec.members.Length;i++)
            {
                var profile=profiles[i];
                var archetype=archetypes[i];
                var type=archetype!=null?archetype.runtimeType:
                    number%Mathf.Max(1,tankEveryRounds)==0&&i==spec.members.Length-1?EnemyType.Tank:EnemyType.Common;
                double braveryRoll=random.NextDouble();
                float bravery=type==EnemyType.Tank?1f:braveryRoll<0.4?1f:braveryRoll<0.75?0.5f:0.2f;
                if(type!=EnemyType.Tank&&bravery>=0.99f&&varyFanaticBraveryForTesting) bravery=1f+(float)random.NextDouble()*0.2f;
                if(profile!=null) bravery=profile.ResolveBravery(bravery);
                spec.members[i]=new NightEnemySpec { archetype=archetype,type=type,bravery=bravery,
                    personality=profile!=null&&profile.uniformPersonality?profile.personality:
                        archetype!=null?archetype.personality:type==EnemyType.Tank?Personality.Objective:
                        random.NextDouble()<Mathf.Min(saboteurBaseChance+saboteurChancePerRound*number,saboteurChanceCap)?Personality.Saboteur:Personality.Objective,
                    temperament=number==1?EnemyTemperament.Disciplined:profile!=null&&profile.uniformTemperament?profile.temperament:
                        type==EnemyType.Tank?EnemyTemperament.Disciplined:(EnemyTemperament)random.Next(4),
                    behavior=number==1?EnemySocialBehavior.Proactive:profile!=null&&profile.uniformBehavior?profile.behavior:
                        type==EnemyType.Tank?EnemySocialBehavior.Proactive:(EnemySocialBehavior)random.Next(3) };
                if(spec.members[i].archetype==null)
                    spec.members[i].archetype=EnemyCatalog.Resolve(type,spec.members[i].personality);
                if(!EnemyFactory.CanSpawn(spec.members[i].archetype))
                    throw new InvalidOperationException("Definição ou prefab de inimigo inválido: "+type);
                spec.members[i].balance=nightDirector.BalanceForNight(spec.members[i].archetype.balance,number);
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
            int available=profile.spawnCount>0?Mathf.Min(profile.spawnCount,groupCount):groupCount;
            if(!valid||required>available||(required<available&&memberWeight<=0f)) continue;
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
