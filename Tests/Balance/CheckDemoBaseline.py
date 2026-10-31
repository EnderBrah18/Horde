"""Audit real serialized demo assets; no Unity runtime or third-party YAML package required."""
from pathlib import Path
import re, math, json
ROOT=Path(__file__).resolve().parents[2]
R=ROOT/'Assets/Resources'
checks=0
def check(ok,label):
 global checks
 checks+=1
 if not ok: raise AssertionError(label)
def text(path):return Path(path).read_text(encoding='utf-8-sig')
def guid(path):return re.search(r'^guid: (\w+)',text(str(path)+'.meta'),re.M).group(1)
def field(s,key,default=None,indent=2):
 m=re.search(r'^'+' '*indent+re.escape(key)+r': (.*)$',s,re.M)
 if not m:return default
 v=m.group(1).strip()
 if v.startswith('"'):return json.loads(v)
 if v=='[]':return []
 if v.startswith('{'):return v
 try:return float(v) if '.' in v or 'e' in v.lower() else int(v)
 except ValueError:return v
def block(s,key,indent=2):
 m=re.search(r'^'+' '*indent+re.escape(key)+r':\s*\n(.*?)(?=^'+' '*indent+r'[A-Za-z_]\w*:|\Z)',s,re.M|re.S)
 return m.group(1) if m else ''
def amounts(s,key,indent=2):
 return {g:int(n) for g,n in re.findall(r'resource: \{fileID: 11400000, guid: (\w+), type: 2\}\s*\n\s*amount: (\d+)',block(s,key,indent))}
def refs(s):return re.findall(r'guid: (\w+)',s)
metas={guid(p.with_suffix('')):p.with_suffix('') for p in (ROOT/'Assets').rglob('*.meta') if p.is_file()}
for p in R.rglob('*.asset'):
 for g in refs(text(p)):check(g in metas,f'{p.name}: reference {g} exists')
resources={p.stem:guid(p) for p in (R/'ResourceDefinitions').rglob('*.asset')}
stock=amounts(text(R/'Economy/DefaultRunEconomy.asset'),'startingStock')
daily=amounts(text(R/'Economy/DefaultRunEconomy.asset'),'dawnGrant')
buildings={p.stem:text(p) for p in (R/'Buildings').glob('*.asset')}
buildByGuid={guid(R/('Buildings/'+n+'.asset')):n for n in buildings}
for name,s in buildings.items():
 check(field(s,'maxHealth',0)>0,name+': positive HP')
 check(bool(amounts(s,'costs')),name+': costs configured')
 for g,n in amounts(s,'costs').items():check(g in resources.values() and n>0,name+': valid positive resource cost')
 if field(s,'kind')==1:
  check(field(s,'towerDamage',0)>0 and field(s,'towerCooldown',0)>0 and field(s,'towerRange',0)>0,name+': positive attack stats')
 seen=set(); current=name
 while current:
  check(current not in seen,name+': upgrade chain has no cycle');seen.add(current)
  upgrade=field(buildings[current],'upgrade','')
  match=re.search(r'guid: (\w+)',upgrade)
  if not match:break
  check(match.group(1) in buildByGuid,name+': upgrade asset exists')
  current=buildByGuid[match.group(1)]
  check(field(buildings[current],'showInBuildMenu',1)==0,name+': upgrade cannot bypass investment as separate build')
check(field(buildings['TorreEspecial'],'showInBuildMenu')==0,'special tower removed from menu')
scene=text(ROOT/'Assets/Scenes/CenaPrincipal.unity')
parts=re.split(r'(?=^--- !u!)',scene,flags=re.M)
special=[p for p in parts if p.startswith('--- !u!1 ') and '  m_Name: Ponto Defesa Especial ' in p]
check(len(special)==4 and all('  m_IsActive: 0' in p for p in special),'all four special areas inactive')
check('startWood:' not in scene and 'dawnCrystalsBase:' not in scene,'scene no longer has competing economy fields')
check(guid(R/'Economy/DefaultRunEconomy.asset') in scene,'scene uses explicit economy asset')
check(guid(R/'NightDirector/DefaultNightDirector.asset') in scene,'scene uses explicit night director')
check(field(scene,'coreHealth')==1200,'core tuned with combat baseline')
# A concrete affordable day-one opening, including staffing the sawmill.
budget=dict(stock)
recipe=[amounts(buildings['Torre'],'costs')]*2+[amounts(text(R/'VillageFacilities/Casa.asset').split('  - displayName:')[1],'costs',4),amounts(text(R/'VillageFacilities/Serraria.asset').split('  - displayName:')[1],'costs',4),amounts(text(R/'NPCs/Morador.asset'),'recruitmentCosts')]
for price in recipe:
 check(bool(price),'opening has valid serialized cost block')
 for g,n in price.items():budget[g]=budget.get(g,0)-n
check(all(n>=0 for n in budget.values()),'two watchtowers + house + sawmill + one worker are affordable on day one')
baseCost=amounts(buildings['Torre'],'costs')
maxTowers=min(stock.get(g,0)//n for g,n in baseCost.items())
check(maxTowers==4,'starting economy cannot fund more than four basic towers even when ignoring all other needs')
check(any(stock.get(g,0)<n for g,n in amounts(buildings['Balista'],'costs').items()),'heavy tower requires further gathering or production')
# Every active spending resource is renewable via expedition, production or dawn grant.
route=text(R/'Expeditions/RotaDaMata.asset')
nodeAssets={guid(p):p for p in (R/'Expeditions/Nodes').glob('*.asset')}
renewable=set(daily)
for g in refs(block(route,'resourceNodes')):
 check(g in nodeAssets,'route references existing resource node')
 node=text(nodeAssets[g]); match=re.search(r'guid: (\w+)',field(node,'resource'))
 renewable.add(match.group(1))
 check(field(node,'minimumStock')>0 and field(node,'maximumStock')>=field(node,'minimumStock'),'valid collectible stock')
for p in (R/'VillageFacilities').glob('*.asset'):
 s=text(p)
 for level in s.split('  - displayName:')[1:]:
  if field(s,'kind')==0:
   check(field(level,'minimumWorkers',0,4)>=1 and field(level,'maximumWorkers',0,4)>=field(level,'minimumWorkers',0,4),p.name+': valid worker thresholds')
   check(bool(amounts(level,'productionPerNight',4)),p.name+': staffed production has outputs')
   renewable.update(amounts(level,'productionPerNight',4))
  else:check(not amounts(level,'productionPerNight',4),p.name+': unfinished services do not grant fake production')
for name,s in buildings.items():
 if name=='TorreEspecial':continue
 for g in amounts(s,'costs'):check(g in renewable,name+': cost resource has a renewable source')
for g in amounts(text(R/'NPCs/Morador.asset'),'recruitmentCosts'):check(g in renewable,'common recruitment resource renewable')
# Composition eligibility and supported behaviors.
enemies={guid(p):text(p) for p in (R/'Enemies').glob('*.asset')}
for g,s in enemies.items():
 check(field(s,'runtimeType') in (0,1),'only implemented physical types are used')
 check(field(s,'personality') in (0,1,2,4),'no unimplemented NPC hunter can strand a wave')
 check(field(block(s,'balance'),'maxHealth',0,4)>0,'enemy has positive HP')
profiles=[]
for p in (R/'NightDirector/Compositions').glob('*.asset'):
 s=text(p);members=[]
 for entry in block(s,'members').split('  - archetype:')[1:]:
  g=re.search(r'guid: (\w+)',entry).group(1)
  check(g in enemies,p.name+': member exists')
  members.append((g,field(entry,'minimumPerGroup',0,4),field(entry,'weight',0,4)))
 check(bool(members),p.name+': nonempty composition')
 check((field(s,'spawnCount',0)==0 or sum(m[1] for m in members)<=field(s,'spawnCount')),p.name+': minima are representable')
 profiles.append((s,members))
for night in range(2,11):
 for count in range(1,121):
  for ratio in [1,1.25,1.6]:
   valid=any(field(s,'firstNight')<=night and field(s,'selectionWeight')>0 and field(s,'minimumPopulationRatio')<=ratio<=field(s,'maximumPopulationRatio') and sum(m[1] for m in ms)<=count and (field(s,'spawnCount',0)==0 or field(s,'spawnCount')<=count) and any(m[2]>0 for m in ms) for s,ms in profiles)
   check(valid,f'profiles cover night {night}, batch {count}, ratio {ratio}, without forced fallback types')
# Independent combat estimates include the first hit at t=0, not just average DPS.
fodder=text(R/'Enemies/Fodder.asset'); tank=text(R/'Enemies/Tank.asset');base=buildings['Torre']
fodderHP=field(block(fodder,'balance'),'maxHealth',0,4)
shots=math.ceil(fodderHP/field(base,'towerDamage'))
ttk=(shots-1)*field(base,'towerCooldown')
check(4<=ttk<=8,'watchtower needs several seconds to kill a common enemy')
check(field(base,'maxHealth')/field(block(fodder,'balance'),'damage',1,4)>=20,'common enemy cannot destroy tower in only a few hits')
check(field(base,'maxHealth')/field(block(tank,'balance'),'damage',1,4)>=12,'Tank is dangerous but cannot erase a tower in only a few hits')
print(f'PASS: {checks} resource/composition/economy checks')
print(f'Day-one watchtower ceiling from resources alone: {maxTowers}; common enemy: {shots} shots, {ttk:.1f}s ideal TTK.')
print('Opening stock remaining after 2 towers + house + sawmill + 1 worker:',{k:budget.get(g,0) for k,g in resources.items() if g in budget})
