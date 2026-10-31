from pathlib import Path
import runpy,json,re
x=runpy.run_path('Tests/Balance/CheckDemoBaseline.py');R=x['R'];field=x['field'];block=x['block'];text=x['text'];guid=x['guid']
def f(v):return str(float(v))+'f'
code=['using System; using System.Reflection; using System.Collections.Generic;','static class DemoBalanceSimulation { static object Field(object o,string n)=>o.GetType().GetField(n,BindingFlags.Public|BindingFlags.Instance).GetValue(o); static void Main(){']
ids={}
for i,p in enumerate(sorted((R/'Enemies').glob('*.asset'))):
 s=text(p);ids[guid(p)]='e'+str(i);b=block(s,'balance')
 code.append('var e'+str(i)+'=new EnemyArchetypeData {displayName='+json.dumps(field(s,'displayName'),ensure_ascii=False)+',runtimeType=(EnemyType)'+str(field(s,'runtimeType'))+',personality=(Personality)'+str(field(s,'personality'))+',bravery='+f(field(s,'bravery'))+',balance=new EnemyBalanceSettings {'+','.join(k+'='+f(field(b,k,indent=4)) for k in ['maxHealth','speed','damage','attackCooldown','attackRange','tacticRadius','knockbackResistance'])+'}};')
byName={p.stem:ids[guid(p)] for p in (R/'Enemies').glob('*.asset')}
code.append('EnemyCatalog.resolver=(t,p)=>t==EnemyType.Tank?'+byName['Tank']+':p==Personality.Saboteur?'+byName['Saboteur']+':'+byName['Fodder']+';')
profileVars=[]
for i,p in enumerate(sorted((R/'NightDirector/Compositions').glob('*.asset'))):
 s=text(p);var='p'+str(i);profileVars.append(var);members=[]
 for entry in block(s,'members').split('  - archetype:')[1:]:
  g=re.search(r'guid: (\w+)',entry).group(1)
  members.append('new EnemyCompositionSlot {archetype='+ids[g]+',minimumPerGroup='+str(field(entry,'minimumPerGroup',0,4))+',weight='+f(field(entry,'weight',0,4))+'}')
 fields=['displayName='+json.dumps(field(s,'displayName'),ensure_ascii=False)]
 for k in ['firstNight','spawnCount']:fields.append(k+'='+str(field(s,k,0)))
 for k in ['selectionWeight','minimumPopulationRatio','maximumPopulationRatio','bravery']:fields.append(k+'='+f(field(s,k)))
 for k in ['uniformTemperament','uniformBehavior','uniformPersonality','uniformBravery','forceTactic']:fields.append(k+'='+('true' if field(s,k) else 'false'))
 for k,typ in [('temperament','EnemyTemperament'),('behavior','EnemySocialBehavior'),('personality','Personality'),('tactic','GroupTactic')]:fields.append(k+'=('+typ+')'+str(field(s,k)))
 fields.append('members=new[]{'+','.join(members)+'}')
 code.append('var '+var+'=new EnemyGroupCompositionData {'+','.join(fields)+'};')
s=text(R/'NightDirector/DefaultNightDirector.asset'); fields=[]
for line in s.splitlines():
 m=re.match(r'^  (\w+): ([\d.]+)$',line)
 if m and m.group(1) not in ['m_ObjectHideFlags','m_Enabled','m_EditorHideFlags']:
  key,value=m.groups();fields.append(key+'='+(value if key in ['firstNightSeed','minimumExtraEnemies','minimumIncreasePercent','maximumIncreasePercent','extraEnemyBatchSize'] else f(value)))
fields.append('groupCompositions=new[]{'+','.join(profileVars)+'}')
code.append('var director=new NightDirectorData {'+','.join(fields)+'};')
code.append('''int[] mins=new int[10],maxs=new int[10];Array.Fill(mins,int.MaxValue);var seen=new HashSet<EnemyArchetypeData>();
for(int run=0;run<100;run++) {
 UnityEngine.Random.Seed=1000+run*97;var game=new Game {nightDirector=director};int firstPeak=45;
 for(int night=1;night<=10;night++) {
  var plan=game.PlanForTest(night);int total=(int)Field(plan,"total"),actual=0;
  if(night==1 && total!=45)throw new Exception("opening population changed");
  if(night<=5)firstPeak=Math.Max(firstPeak,total);
  if(night==6 && game.CurrentNightMinimum!=firstPeak)throw new Exception("block floor changed incorrectly");
  if(total<45 || total>110)throw new Exception("population escaped configured demo envelope");
  foreach(var group in (Array)Field(plan,"groups")) foreach(var member in (Array)Field(group,"members")) {
   actual++;var archetype=(EnemyArchetypeData)Field(member,"archetype");seen.Add(archetype);
   if((Personality)Field(member,"personality")==Personality.NpcHunter)throw new Exception("reserved personality in active wave");
   var balance=(EnemyBalanceSettings)Field(member,"balance");
   if(balance.maxHealth<=0 || balance.maxHealth>archetype.balance.maxHealth*1.701f)throw new Exception("invalid health growth");
  }
  if(actual!=total || !game.CompleteForTest(night,total))throw new Exception("population mismatch");
  mins[night-1]=Math.Min(mins[night-1],total);maxs[night-1]=Math.Max(maxs[night-1],total);
 }
}
Console.WriteLine("PASS: 100 runs x 10 nights with serialized resource profiles; distinct enemy definitions: "+seen.Count);
for(int n=0;n<10;n++)Console.WriteLine("Night "+(n+1)+": "+mins[n]+"-"+maxs[n]);
''')
code.append('if(seen.Count!='+str(len(ids))+')throw new Exception("a definition was never reached by night 10");')
code.append('}}')
Path('Temp/DemoBalanceSimulation.cs').write_text('\n'.join(code),encoding='utf-8')
refRoot=Path('C:/Program Files/dotnet/packs/Microsoft.NETCore.App.Ref')
versions=sorted(refRoot.iterdir(),key=lambda p:tuple(int(n) for n in p.name.split('.') if n.isdigit()))
references=sorted((versions[-1]/'ref/net10.0').glob('*.dll'))
sources=['Tests/Nights/NightDirectorTests.cs','Assets/Scripts/Enemies/Nights/NightPopulationHistory.cs','Assets/Scripts/Enemies/Nights/NightDirectorData.cs','Assets/Scripts/Enemies/Definitions/EnemyArchetypeData.cs','Assets/Scripts/Enemies/Nights/EnemyGroupCompositionData.cs','Assets/Scripts/Enemies/Nights/Game.NightComposition.cs','Assets/Scripts/Enemies/EnemyBalanceSettings.cs','Assets/Scripts/Expeditions/VillageProgressionRules.cs','Assets/Scripts/Enemies/EnemyTargetRules.cs','Assets/Scripts/Enemies/Nights/NightDeploymentRules.cs','Assets/Scripts/Enemies/PlayerHuntMemory.cs','Assets/Scripts/Enemies/EnemyPositioningRules.cs','Assets/Scripts/Enemies/FormationSlots.cs','Temp/DemoBalanceSimulation.cs']
source='\n'.join(['-nostdlib+','-target:exe','-out:Temp/DemoBalanceSimulation.dll','-main:DemoBalanceSimulation']+['-r:"'+str(p)+'"' for p in references]+sources)
Path('Temp/DemoBalanceSimulation.rsp').write_text(source,encoding='utf-8')
Path('Temp/DemoBalanceSimulation.runtimeconfig.json').write_text('{"runtimeOptions":{"tfm":"net10.0","framework":{"name":"Microsoft.NETCore.App","version":"10.0.0"}}}',encoding='utf-8')
