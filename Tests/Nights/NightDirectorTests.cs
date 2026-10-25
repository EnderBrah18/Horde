using System;
using System.Reflection;
using System.Text;

// Real planner/data/history. Only Unity runtime types and the surrounding Game
// scene context are substituted; movement/combat must be tested in Play Mode.
namespace UnityEngine
{
    public class ScriptableObject {}
    public struct Vector3 { public Vector3(float x,float y,float z) {} }
    public static class Mathf
    {
        public static int Max(int a,int b)=>Math.Max(a,b);
        public static int Min(int a,int b)=>Math.Min(a,b);
        public static float Max(float a,float b)=>Math.Max(a,b);
        public static float Min(float a,float b)=>Math.Min(a,b);
    }
    public static class Random { public static int Seed=41; public static int Range(int a,int b)=>++Seed; }
    public static class Resources { public static T Load<T>(string path) where T:class=>null; }
    public sealed class CreateAssetMenu : Attribute { public string menuName,fileName; }
    public sealed class HeaderAttribute : Attribute { public HeaderAttribute(string value) {} }
    public sealed class TooltipAttribute : Attribute { public TooltipAttribute(string value) {} }
    public sealed class MinAttribute : Attribute { public MinAttribute(float value) {} }
    public sealed class RangeAttribute : Attribute { public RangeAttribute(float min,float max) {} }
    public sealed class TextAreaAttribute : Attribute {}
}
public enum EnemyType {Common,Tank}
public enum Personality {Objective,Hunter}
public enum EnemyTemperament {Disciplined,Loyal,Wild,Berserker}
public enum EnemySocialBehavior {Proactive,Passive,Rebellious}
public enum GroupTactic {Aggressive,Tactical}
public partial class Game
{
    public int initialWaves=3,groupsPerWave=3,nightsPerExtraWave=2,maxWaves=5;
    public int baseCommonEnemies=4,commonsPerRound=1,tankEveryRounds=2;
    public float tacticalGroupChance=0.7f,hunterBaseChance=0.3f,hunterChancePerRound=0.05f,hunterChanceCap=0.6f;
    public bool varyFanaticBraveryForTesting=true;
    public object PlanForTest(int night)=>PlanNight(night);
}
static class NightDirectorTests
{
    static int checks;
    static void Check(bool value,string label) { checks++; if(!value) throw new Exception(label); }
    static object Field(object obj,string name)=>obj.GetType().GetField(name,BindingFlags.Public|BindingFlags.Instance).GetValue(obj);
    static string Signature(object plan)
    {
        var result=new StringBuilder();
        foreach(var group in (Array)Field(plan,"groups"))
        {
            result.Append(Field(group,"tactic"));
            foreach(var member in (Array)Field(group,"members"))
                foreach(var field in new[]{"type","personality","bravery","temperament","behavior"}) result.Append(Field(member,field)).Append(';');
        }
        return result.ToString();
    }
    static void Main()
    {
        var history=new NightPopulationHistory(10);
        Check(history.MinimumFor(1)==10 && history.MinimumFor(5)==10,"first block floor is opening count");
        Check(history.RecordCompleted(1,10) && history.RecordCompleted(2,23),"record completed nights");
        Check(history.MinimumFor(3)==10 && history.MinimumFor(5)==10,"peak does not change floor mid-block");
        history.RecordCompleted(3,12); history.RecordCompleted(4,20); history.RecordCompleted(5,10);
        Check(history.MinimumFor(6)==23 && history.MinimumFor(10)==23,"second block uses largest first-block night exactly");
        Check(!history.RecordCompleted(2,999) && history.MinimumFor(6)==23,"duplicate completion cannot inflate record");
        Check(!history.RecordCompleted(6,22),"cannot record below block floor");
        history.RecordCompleted(6,23); history.RecordCompleted(7,47);
        Check(history.MinimumFor(8)==23 && history.MinimumFor(10)==23,"second block floor remains fixed");
        Check(history.MinimumFor(11)==47,"third block inherits second-block maximum");
        Check(new NightPopulationHistory(10).MinimumFor(6)==10,"new run does not inherit previous history");
        var random=new Random(123);
        bool[] seen=new bool[31];
        for(int i=0;i<5000;i++)
        {
            int count=NightPopulationHistory.RollCount(10,10,50,200,5,0.25,random);
            if(count!=10 && count!=20 && count!=25 && count!=30) throw new Exception("Invalid small increase: "+count);
            seen[count]=true;
        }
        Check(seen[10]&&seen[20]&&seen[25]&&seen[30],"sampling includes baseline and substantial quantized increases only");
        Check(NightPopulationHistory.RollCount(23,10,50,200,5,1,new Random(1))==23,"minimum is not rounded to a group multiple");
        var counts=NightPopulationHistory.Distribute(23,9); int sum=0,small=999,large=0;
        foreach(int count in counts) {sum+=count;small=Math.Min(small,count);large=Math.Max(large,count);}
        Check(sum==23 && large-small<=1 && small>0,"whole population distributed exactly over nonempty groups");
        Check(NightPopulationHistory.RollCount(int.MaxValue,10,50,200,5,0,new Random(1))==int.MaxValue,"integer limit cannot overflow below floor");
        var director=new NightDirectorData();
        var firstGame=new Game {nightDirector=director}; var opening=firstGame.PlanForTest(1);
        Check((int)Field(opening,"total")==45 && ((Array)Field(opening,"groups")).Length==9,"opening keeps three waves of three groups and forty-five enemies");
        UnityEngine.Random.Seed=999;
        Check(Signature(opening)==Signature(new Game {nightDirector=director}.PlanForTest(1)),"opening composition independent of run and spawn random streams");
        var custom=new Game {nightDirector=director,groupsPerWave=2,maxWaves=1};
        Check((int)Field(custom.PlanForTest(1),"total")==10,"opening honors configured wave cap and group count");
        var elite=new EnemyArchetypeData {runtimeType=EnemyType.Common};
        var tank=new EnemyArchetypeData {runtimeType=EnemyType.Tank};
        var profile=new EnemyGroupCompositionData {displayName="Escort",uniformTemperament=true,temperament=EnemyTemperament.Loyal,
            uniformBehavior=true,behavior=EnemySocialBehavior.Passive,forceTactic=true,tactic=GroupTactic.Tactical,
            members=new[]{new EnemyCompositionSlot {archetype=elite,minimumPerGroup=1,weight=1},new EnemyCompositionSlot {archetype=tank,minimumPerGroup=1,weight=0}}};
        director.groupCompositions=new[]{profile}; director.repeatMinimumChance=1;
        var planned=new Game {nightDirector=director}.PlanForTest(2);
        bool valid=true; int plannedTotal=0;
        foreach(var group in (Array)Field(planned,"groups"))
        {
            int tanks=0;
            foreach(var member in (Array)Field(group,"members"))
            {
                plannedTotal++;
                if((EnemyType)Field(member,"type")==EnemyType.Tank)tanks++;
                valid &= (EnemyTemperament)Field(member,"temperament")==EnemyTemperament.Loyal &&
                    (EnemySocialBehavior)Field(member,"behavior")==EnemySocialBehavior.Passive;
            }
            valid &= tanks==1 && (GroupTactic)Field(group,"tactic")==GroupTactic.Tactical;
        }
        Check(valid && plannedTotal==45,"escort guarantees one tank and uniform social settings without changing total");
        Check(Signature(opening)==Signature(new Game {nightDirector=director}.PlanForTest(1)),"random composition assets cannot alter opening");
        profile.members[0].minimumPerGroup=100;
        Check((int)Field(new Game {nightDirector=director}.PlanForTest(2),"total")==45,"incompatible profile falls back without changing quota");
        director.repeatMinimumChance=0;
        for(int n=2;n<=5;n++)
        {
            var game=new Game {nightDirector=director}; var p=game.PlanForTest(n); int actual=0;
            foreach(var group in (Array)Field(p,"groups")) actual+=((Array)Field(group,"members")).Length;
            Check(actual==(int)Field(p,"total") && actual>=45,"planned count remains exact despite changing number of waves");
        }
        Console.WriteLine("PASS: "+checks+" night director checks (including 5,000 population samples)");
    }
}
