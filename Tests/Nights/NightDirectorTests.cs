using System;
using System.Reflection;
using System.Text;

// Real planner/data/history. Only Unity runtime types and the surrounding Game
// scene context are substituted; movement/combat must be tested in Play Mode.
namespace UnityEngine
{
    public class ScriptableObject {}
    public class GameObject {}
    public enum PrimitiveType {Sphere,Capsule,Cylinder,Cube}
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
public enum Personality {Objective=0,Saboteur=1,PlayerHunter=2,NpcHunter=3,Simple=4}
public enum StructureKind {Wall=0,Tower=1,Core=2,Facility=3}
public enum EnemyTemperament {Disciplined,Loyal,Wild,Berserker}
public enum EnemySocialBehavior {Proactive,Passive,Rebellious}
public enum GroupTactic {Aggressive,Tactical}
public static class EnemyCatalog
{
    public static Func<EnemyType,Personality,EnemyArchetypeData> resolver;
    public static EnemyArchetypeData Resolve(EnemyType type,Personality personality)=>resolver!=null?resolver(type,personality):new EnemyArchetypeData {runtimeType=type,personality=personality};
}
public static class EnemyFactory
{
    public static bool CanSpawn(EnemyArchetypeData definition)=>definition!=null&&definition.balance!=null;
}
public partial class Game
{
    public int initialWaves=3,groupsPerWave=3,nightsPerExtraWave=2,maxWaves=5;
    public int baseCommonEnemies=4,commonsPerRound=1,tankEveryRounds=2;
    public float tacticalGroupChance=0.7f,saboteurBaseChance=0.3f,saboteurChancePerRound=0.05f,saboteurChanceCap=0.6f;
    public bool varyFanaticBraveryForTesting=true;
    public object PlanForTest(int night)=>PlanNight(night);
    public bool CompleteForTest(int night,int total)=>nightPopulation.RecordCompleted(night,total);
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
        Check(!EnemyTargetRules.PreferPlayer(Personality.PlayerHunter,true,false),"player hunter keeps core strategy before detecting a victim");
        Check(EnemyTargetRules.PreferPlayer(Personality.Objective,false,true),"core-oriented type falls back to player without core");
        Check(EnemyTargetRules.PreferPlayer(Personality.Saboteur,false,true),"tower-oriented type falls back to player without core");
        Check(!EnemyTargetRules.PreferPlayer(Personality.Objective,true,true),"existing core behavior retained in village");
        Check(!EnemyTargetRules.PreferPlayer(Personality.Objective,false,false),"fallback can be disabled per asset");
        Check(EnemyTargetRules.IsLocalStructure(true,true,true),"local attackable structure accepted");
        Check(!EnemyTargetRules.IsLocalStructure(true,false,true),"core from another scene cannot become local target");
        Check(!EnemyTargetRules.IsLocalStructure(true,true,false),"core outside navigation region cannot become target");
        Check(!EnemyTargetRules.IsLocalStructure(false,true,true),"invulnerable ruins remain excluded");
        foreach (Personality personality in Enum.GetValues(typeof(Personality)))
            foreach (StructureKind kind in Enum.GetValues(typeof(StructureKind)))
                Check(EnemyTargetRules.MatchesStructure(personality,kind)==
                    (((personality==Personality.Objective || personality==Personality.PlayerHunter) && kind==StructureKind.Core) ||
                     (personality==Personality.Saboteur && kind==StructureKind.Tower)),
                    "rigid personality excludes unrelated structures: "+personality+" / "+kind);
        Check(!EnemyTargetRules.CanAcquirePlayer(Personality.PlayerHunter,true,false,1000,10),"player hunter cannot acquire unseen distant victim");
        Check(!EnemyTargetRules.CanAcquirePlayer(Personality.Objective,true,true,1,10),"objective ignores nearby player while core exists");
        Check(!EnemyTargetRules.CanAcquirePlayer(Personality.Saboteur,true,true,1,10),"saboteur ignores nearby player while core exists");
        Check(!EnemyTargetRules.CanAcquirePlayer(Personality.Objective,false,true,100,10),"context fallback retains perception");
        Check(!EnemyTargetRules.CanAcquirePlayer(Personality.Saboteur,false,false,1,10),"saboteur without fallback waits rather than switching to player");
        Check(!EnemyTargetRules.CanAcquirePlayer(Personality.NpcHunter,false,true,1,10),"reserved NPC hunter does not substitute player even without core");
        foreach(StructureKind kind in Enum.GetValues(typeof(StructureKind)))
            Check(!EnemyTargetRules.CanSelectStructure(Personality.NpcHunter,kind,1,10),"reserved NPC hunter has no implemented structure target");
        Check(EnemyTargetRules.CanAcquirePlayer(Personality.Simple,true,false,5,10),"simple perceives nearby player despite a core existing");
        Check(!EnemyTargetRules.CanAcquirePlayer(Personality.Simple,false,true,11,10),"simple cannot hunt distant player");
        Check(EnemyTargetRules.CanSelectStructure(Personality.Simple,StructureKind.Facility,5,10),"simple may choose nearby facility");
        Check(EnemyTargetRules.CanSelectStructure(Personality.Simple,StructureKind.Tower,10,10),"simple perception includes boundary");
        Check(!EnemyTargetRules.CanSelectStructure(Personality.Simple,StructureKind.Tower,11,10),"simple does not globally know distant towers");
        Check(!EnemyTargetRules.CanSelectStructure(Personality.Simple,StructureKind.Wall,1,10),"simple treats walls as obstacles rather than attractions");
        Check(EnemyTargetRules.CanSelectStructure(Personality.Objective,StructureKind.Core,1000,10),"rigid objective still acquires distant core");
        Check(EnemyTargetRules.CanSelectStructure(Personality.Saboteur,StructureKind.Tower,1000,10),"rigid saboteur still acquires distant tower");
        var hunt=new PlayerHuntMemory<int>();
        hunt.Update(true,false,100,0,5,20);
        Check(!hunt.HasDestination,"spawn outside detection does not reveal player position");
        hunt.Update(true,true,1,1,5,20);
        Check(hunt.Phase==PlayerHuntPhase.Tracking && hunt.Destination==1,"detection starts hunt");
        hunt.Update(true,false,10,2,5,20);
        Check(hunt.Phase==PlayerHuntPhase.GracePeriod && hunt.Destination==10,"leaving radius starts grace period");
        hunt.Update(true,false,20,6.99f,5,20);
        Check(hunt.Phase==PlayerHuntPhase.GracePeriod && hunt.Destination==20,"continues tracking current position before deadline");
        hunt.Update(true,false,30,7,5,20);
        Check(hunt.Phase==PlayerHuntPhase.Investigating && hunt.Destination==30,"snapshot is captured at end of five seconds, not on exit");
        hunt.Update(true,false,999,8,5,20);
        Check(hunt.Destination==30,"investigation does not read updated hidden victim position");
        hunt.CompleteInvestigation();
        Check(!hunt.HasDestination,"arrival without reacquisition ends hunt and allows core fallback");
        hunt.Update(true,false,999,9,5,20);
        Check(!hunt.HasDestination,"finished hunt cannot reacquire outside perception");
        hunt.Update(true,true,2,10,5,20);
        hunt.Update(true,false,3,11,5,20);
        hunt.Update(true,true,4,14,5,20);
        Check(hunt.Phase==PlayerHuntPhase.Tracking && hunt.Destination==4,"redetection cancels active timer");
        hunt.Update(true,false,5,15,5,20);
        hunt.Update(true,false,6,19,5,20);
        Check(hunt.Phase==PlayerHuntPhase.GracePeriod,"new exit starts a full new countdown");
        hunt.Update(true,false,7,20,5,20);
        Check(hunt.Phase==PlayerHuntPhase.Investigating && hunt.Destination==7,"new countdown freezes its own final position");
        hunt.Update(true,true,8,21,5,20);
        Check(hunt.Phase==PlayerHuntPhase.Tracking,"redetection while investigating resumes pursuit");
        hunt.Update(false,false,0,22,5,20);
        Check(!hunt.HasDestination,"dead or invalid victim clears memory");
        hunt.Update(true,true,1,30,0,20);
        hunt.Update(true,false,2,31,0,20);
        Check(hunt.Phase==PlayerHuntPhase.Investigating && hunt.Destination==2,"zero grace freezes immediately on exit");
        hunt.Update(true,false,3,51,0,20);
        Check(!hunt.HasDestination,"unreachable investigation has finite timeout and allows core fallback");
        var tracker=new PlayerHuntMemory<int>();
        tracker.Update(true,false,100,0,5,20,PlayerHuntStyle.Tracker);
        Check(!tracker.HasDestination,"tracker cannot acquire victim outside detection on spawn");
        tracker.Update(true,true,1,1,5,20,PlayerHuntStyle.Tracker);
        tracker.Update(true,false,40,100,5,20,PlayerHuntStyle.Tracker);
        Check(tracker.Phase==PlayerHuntPhase.Tracking && tracker.Destination==40,"tracker follows current position beyond grace and investigation timers");
        tracker.Update(true,false,90,10000,5,20,PlayerHuntStyle.Tracker);
        Check(tracker.Destination==90 && tracker.Phase==PlayerHuntPhase.Tracking,"tracker has no hidden timeout or frozen position");
        tracker.CompleteInvestigation();
        Check(tracker.HasDestination,"tracker does not stop when passing an old position");
        tracker.Update(false,false,0,10001,5,20,PlayerHuntStyle.Tracker);
        Check(!tracker.HasDestination,"invalid victim clears tracker memory");
        tracker.Update(true,false,100,10002,5,20,PlayerHuntStyle.Tracker);
        Check(!tracker.HasDestination,"tracker must detect again after victim was lost as invalid");
        tracker.Update(true,true,1,10003,5,20,PlayerHuntStyle.Tracker);
        tracker.Update(true,false,2,10004,5,20,PlayerHuntStyle.Stalker);
        Check(tracker.Phase==PlayerHuntPhase.GracePeriod,"switching tracker to stalker starts grace on lost detection");
        Check(EnemyPositioningRules.CanCoverBehind(true,true,true,true,false,true,8,10),"living Tank from same navigation in detection range is eligible");
        Check(!EnemyPositioningRules.CanCoverBehind(false,true,true,true,false,true,8,10),"dead Tank cannot provide cover");
        Check(!EnemyPositioningRules.CanCoverBehind(true,false,true,true,false,true,8,10),"fleeing or rallying Tank is excluded");
        Check(!EnemyPositioningRules.CanCoverBehind(true,true,false,true,false,true,8,10),"Tank from inactive navigation cannot attract follower");
        Check(!EnemyPositioningRules.CanCoverBehind(true,true,true,false,false,true,8,10),"Tank in another loaded scene is excluded");
        Check(!EnemyPositioningRules.CanCoverBehind(true,true,true,true,true,true,0,10),"Tank cannot cover behind itself");
        Check(!EnemyPositioningRules.CanCoverBehind(true,true,true,true,false,false,8,10),"non Tank cannot be cover anchor");
        Check(!EnemyPositioningRules.CanCoverBehind(true,true,true,true,false,true,11,10),"new Tank must be detected locally");
        Check(EnemyPositioningRules.ShouldLeaveGroup(true,false,false),"member leaves own group to cover behind external Tank");
        Check(!EnemyPositioningRules.ShouldLeaveGroup(true,true,false),"leader retains group while following external Tank");
        Check(!EnemyPositioningRules.ShouldLeaveGroup(true,false,true),"Tank in own group does not cause departure");
        Check(!EnemyPositioningRules.ShouldLeaveGroup(false,false,false),"solo follower remains ungrouped");
        var sharedCover=new FormationSlots<string>();
        int oldGroupSlot=sharedCover.Claim("tactical-member");
        int individualSlot=sharedCover.Claim("individual-from-west");
        int externalGroupSlot=sharedCover.Claim("external-group-member");
        Check(oldGroupSlot!=individualSlot && oldGroupSlot!=externalGroupSlot && individualSlot!=externalGroupSlot,
            "old tactical group, individual and external escort reserve unique shared slots");
        Check(sharedCover.Claim("tactical-member")==oldGroupSlot,"same follower retains slot across requests and sources");
        sharedCover.Release("individual-from-west");
        Check(sharedCover.Claim("new-from-north")==individualSlot,"new follower reuses vacancy without renumbering remaining followers");
        Check(sharedCover.Claim("external-group-member")==externalGroupSlot,"removing another follower preserves occupied slots");
        sharedCover.Release("missing-follower");
        Check(sharedCover.Count==3,"releasing unknown follower is harmless");
        var offsets=new System.Collections.Generic.HashSet<string>();
        for(int slot=0;slot<200;slot++)
            Check(offsets.Add(FormationSlots<string>.Row(slot)+":"+FormationSlots<string>.Column(slot)),"shared rows and columns never reuse a physical offset");
        var slotRandom=new Random(5481);
        var activeClaims=new System.Collections.Generic.Dictionary<int,int>();
        var allocator=new FormationSlots<int>();
        for(int iteration=0;iteration<5000;iteration++)
        {
            int follower=slotRandom.Next(100);
            if(slotRandom.Next(3)==0) {allocator.Release(follower);activeClaims.Remove(follower);}
            else
            {
                int assigned=allocator.Claim(follower);
                if(activeClaims.TryGetValue(follower,out int previous) && assigned!=previous)throw new Exception("live follower changed slot");
                activeClaims[follower]=assigned;
            }
            var uniqueSlots=new System.Collections.Generic.HashSet<int>(activeClaims.Values);
            if(uniqueSlots.Count!=activeClaims.Count || allocator.Count!=activeClaims.Count)throw new Exception("duplicate or stale reservation");
        }
        Check(true,"five thousand mixed formation arrivals and departures retain unique stable slots");
        var hunterProfile=new EnemyGroupCompositionData {uniformPersonality=true,personality=Personality.PlayerHunter,
            members=new[]{new EnemyCompositionSlot {archetype=new EnemyArchetypeData {runtimeType=EnemyType.Tank},minimumPerGroup=1,weight=1}}};
        var focusedPlan=new Game {nightDirector=new NightDirectorData {groupCompositions=new[]{hunterProfile}}}.PlanForTest(2);
        bool foundFocusedTank=false;
        foreach(var batch in (Array)Field(focusedPlan,"groups"))
            foreach(var member in (Array)Field(batch,"members"))
                if((EnemyType)Field(member,"type")==EnemyType.Tank)
                { foundFocusedTank=true; Check((Personality)Field(member,"personality")==Personality.PlayerHunter,"composition can give a Tank the player-hunter focus"); }
        Check(foundFocusedTank,"focused composition contains Tanks");
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
        // Pin the dispersed deployment while testing composition filling in isolation.
        var director=new NightDirectorData {singleAssaultChance=0,concentratedWaveChance=0,
            dispersedGroupWeight=1,balancedGroupWeight=0,armyGroupWeight=0,armyWeightPerNight=0};
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
        profile.uniformBravery=true; profile.bravery=0.65f;
        var bravePlan=new Game {nightDirector=director}.PlanForTest(2);
        bool allForced=true, forcedTank=false;
        foreach(var batch in (Array)Field(bravePlan,"groups"))
            foreach(var member in (Array)Field(batch,"members"))
            {
                allForced &= (float)Field(member,"bravery")==0.65f;
                forcedTank |= (EnemyType)Field(member,"type")==EnemyType.Tank && (float)Field(member,"bravery")==0.65f;
            }
        Check(allForced && forcedTank,"uniform bravery overrides random values and Tank defaults");
        profile.bravery=-1f; Check(profile.ResolveBravery(1f)==0f,"forced bravery clamps lower boundary");
        profile.bravery=2f; Check(profile.ResolveBravery(1f)==1.2f,"forced bravery clamps upper boundary");
        profile.bravery=0.65f;
        Check(Signature(opening)==Signature(new Game {nightDirector=director}.PlanForTest(1)),"random composition assets cannot alter opening");
        profile.uniformBravery=false;
        Check(profile.ResolveBravery(0.2f)==0.2f && profile.ResolveBravery(1f)==1f,"disabled toggle preserves existing bravery");
        var normalPlan=new Game {nightDirector=director}.PlanForTest(2);
        bool originalBravery=true;
        foreach(var batch in (Array)Field(normalPlan,"groups"))
            foreach(var member in (Array)Field(batch,"members"))
            {
                float value=(float)Field(member,"bravery");
                originalBravery &= (EnemyType)Field(member,"type")==EnemyType.Tank ? value==1f :
                    value==0.2f || value==0.5f || (value>=1f && value<=1.2f);
            }
        Check(originalBravery,"disabled composition keeps night bravery distribution");
        profile.spawnCount=2;
        var smallProfile=new EnemyGroupCompositionData {displayName="Small wild pack",spawnCount=3,
            uniformTemperament=true,temperament=EnemyTemperament.Wild,
            members=new[]{new EnemyCompositionSlot {archetype=elite,weight=1}}};
        director.groupCompositions=new[]{profile,smallProfile};
        var mixed=new Game {nightDirector=director}.PlanForTest(2);
        bool foundMixed=false;
        foreach(var group in (Array)Field(mixed,"groups"))
        {
            bool loyal=false,wild=false;
            var members=(Array)Field(group,"members");
            Check(members.Length<=5,"batch capacity stays five independently of composition");
            foreach(var member in members)
            {
                var temperament=(EnemyTemperament)Field(member,"temperament");
                loyal |= temperament==EnemyTemperament.Loyal;
                wild |= temperament==EnemyTemperament.Wild;
            }
            foundMixed |= loyal && wild;
        }
        Check(foundMixed,"one batch combines small compositions and preserves each social profile");
        director.groupCompositions=new[]{profile};
        profile.spawnCount=0;
        profile.members[0].minimumPerGroup=100;
        Check((int)Field(new Game {nightDirector=director}.PlanForTest(2),"total")==45,"incompatible profile falls back without changing quota");
        director.repeatMinimumChance=0;
        for(int n=2;n<=5;n++)
        {
            var game=new Game {nightDirector=director}; var p=game.PlanForTest(n); int actual=0;
            foreach(var group in (Array)Field(p,"groups"))
            {
                int count=((Array)Field(group,"members")).Length;
                Check(count>0 && count<=5,"larger nights add batches instead of increasing batch size");
                actual+=count;
            }
            Check(actual==(int)Field(p,"total") && actual>=45,"planned count remains exact despite changing number of waves");
        }
        var run=new Game {nightDirector=director};
        int firstPeak=45, secondPeak=0;
        for(int night=1;night<=11;night++)
        {
            var plan=run.PlanForTest(night); int total=(int)Field(plan,"total");
            if(night<=5)firstPeak=Math.Max(firstPeak,total);
            if(night>=6 && night<=10)secondPeak=Math.Max(secondPeak,total);
            if(night==6)Check(run.CurrentNightMinimum==firstPeak,"actual planner inherits first block peak on sixth night");
            if(night==11)Check(run.CurrentNightMinimum==secondPeak,"actual planner inherits second block peak on eleventh night");
            if(!run.CompleteForTest(night,total))throw new Exception("planned complete night rejected");
        }
        var deploymentSettings=new NightDirectorData();
        var deploymentRandom=new Random(850);
        bool sawArmy=false,sawDispersed=false,sawUneven=false,sawWholeNight=false;
        for(int sample=0;sample<5000;sample++)
        {
            int total=1+sample%150;
            var deployment=NightDeploymentRules.Plan(total,5,3,5,20,deploymentSettings,deploymentRandom);
            int actual=0;
            foreach(var wave in deployment)
            {
                int smallest=int.MaxValue,biggest=0;
                foreach(int count in wave)
                {
                    if(count<1)throw new Exception("empty deployment group");
                    actual+=count;smallest=Math.Min(smallest,count);biggest=Math.Max(biggest,count);
                    sawArmy |= count>=30;
                }
                sawDispersed |= wave.Length>=6;
                sawUneven |= biggest-smallest>=10;
            }
            sawWholeNight |= deployment.Length==1 && deployment[0].Length==1 && total>=60;
            if(actual!=total)throw new Exception("deployment changed population quota");
        }
        Check(sawArmy && sawDispersed && sawUneven && sawWholeNight,"later nights support armies, small groups, uneven support and one whole assault");
        deploymentSettings.singleAssaultChance=0;deploymentSettings.concentratedWaveChance=0;
        deploymentSettings.dispersedGroupWeight=0;deploymentSettings.balancedGroupWeight=1;
        deploymentSettings.armyGroupWeight=0;deploymentSettings.armyWeightPerNight=0;
        var balancedDeployment=NightDeploymentRules.Plan(60,1,3,5,20,deploymentSettings,new Random(1));
        Check(balancedDeployment[0].Length==3 && balancedDeployment[0][0]==20 && balancedDeployment[0][2]==20,
            "sixty can spawn as exactly three groups of twenty");
        deploymentSettings.singleAssaultChance=1;
        var fullDeployment=NightDeploymentRules.Plan(90,5,3,5,20,deploymentSettings,new Random(1));
        Check(fullDeployment.Length==1 && fullDeployment[0][0]==90,"single assault puts exact entire night in one group");
        var fixedDeployment=NightDeploymentRules.Plan(45,3,3,5,1,deploymentSettings,new Random(1));
        Check(fixedDeployment.Length==3 && fixedDeployment[0].Length==3 && fixedDeployment[0][0]==5,
            "opening ignores later-night concentration settings");
        deploymentSettings.singleAssaultChance=0;deploymentSettings.concentratedWaveChance=1;
        deploymentSettings.minimumDominantShare=2f/3f;deploymentSettings.maximumDominantShare=2f/3f;
        var concentrated=NightDeploymentRules.Plan(90,4,1,5,20,deploymentSettings,new Random(1));
        int bigWaves=0,smallWaves=0;
        foreach(var wave in concentrated) { if(wave[0]==60)bigWaves++; if(wave[0]==10)smallWaves++; }
        Check(bigWaves==1 && smallWaves==3,"ninety supports a sixty-strong force plus three groups of ten");
        var source=new EnemyBalanceSettings {maxHealth=100,damage=10,speed=4,attackCooldown=2};
        var scaling=new NightDirectorData();
        var first=scaling.BalanceForNight(source,1);
        var twentieth=scaling.BalanceForNight(source,20);
        Check(first.maxHealth==100 && first.damage==10 && first.speed==4 && first.attackCooldown==2,"opening has no stat increase");
        Check(Math.Abs(twentieth.maxHealth-252)<0.001 && Math.Abs(twentieth.damage-19.5)<0.001,
            "twentieth night scales health and damage from original archetype");
        Check(twentieth.speed>source.speed && twentieth.attackCooldown<source.attackCooldown,"speed and attack rate also progress");
        var capped=scaling.BalanceForNight(source,1000);
        Check(capped.maxHealth==400 && capped.damage==30 && capped.speed<=4.601 && capped.attackCooldown>=2/1.351,
            "stat caps prevent unbounded movement and attack scaling");
        Check(source.maxHealth==100 && source.damage==10 && source.attackCooldown==2 && !ReferenceEquals(source,twentieth),
            "scaling never mutates archetype assets");
        var lateDirector=new NightDirectorData {repeatMinimumChance=1,singleAssaultChance=1};
        var latePlan=new Game {nightDirector=lateDirector}.PlanForTest(20);
        var lateGroups=(Array)Field(latePlan,"groups");
        Check(lateGroups.Length==1 && ((Array)Field(lateGroups.GetValue(0),"members")).Length==45,
            "real planner accepts a group larger than five in late night");
        var lateMember=((Array)Field(lateGroups.GetValue(0),"members")).GetValue(0);
        Check(((EnemyBalanceSettings)Field(lateMember,"balance")).maxHealth>25,"real planner passes scaled stats to spawn specification");
        Console.WriteLine("PASS: "+checks+" night director checks (including 5,000 population samples)");
    }
}
