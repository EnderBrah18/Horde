from pathlib import Path
import re

root = Path(__file__).resolve().parents[2]
checks = 0

def check(value, label):
    global checks
    checks += 1
    if not value:
        raise AssertionError(label)

guids = {}
for location in ['Assets', 'Packages', 'Library/PackageCache']:
    for meta in (root / location).rglob('*.meta'):
        match = re.search(r'^guid: (\w+)', meta.read_text(encoding='utf-8-sig'), re.M)
        if match:
            guids[match[1]] = meta

for relative in ['Assets/Scenes/MenuPrincipal.unity', 'Assets/Scenes/CenaPrincipal.unity', 'Assets/Scenes/Expedicoes.unity']:
    text = (root / relative).read_text(encoding='utf-8-sig')
    ids = re.findall(r'^--- !u!\d+ &(\d+)', text, re.M)
    check(len(ids) == len(set(ids)), relative + ': unique object IDs')
    defined = set(ids)
    # External assets and engine built-ins do not resolve to local scene records.
    for reference in re.findall(r'\{fileID: (\d+)\}', text):
        check(reference == '0' or reference in defined, relative + ': dangling reference ' + reference)
    for reference in re.findall(r'guid: (\w+)', text):
        check(reference.startswith('0000000000000000') or reference in guids, relative + ': missing asset ' + reference)
    if relative.endswith('CenaPrincipal.unity'):
        check('m_Name: Posto_Expedicoes' in text, 'physical exit in scene')
        check('m_Name: NPC_Morador' in text and 'm_Name: NPC_Artur_Ferreiro' in text, 'existing NPCs preserved')
        check('  groupsPerWave: 3' in text and '  initialWaves: 3' in text and '  maxWaves: 5' in text, 'three groups per wave and 3 to 5 waves')
    if relative.endswith('MenuPrincipal.unity'):
        check('Assembly-CSharp::MainMenu' in text and '  gameplayScene: CenaPrincipal' in text, 'main menu starts village scene')
        check('Assembly-CSharp::Game\n' not in text and 'Assembly-CSharp::VillageProduction' not in text, 'menu does not start gameplay simulation')

build = (root / 'ProjectSettings/EditorBuildSettings.asset').read_text()
for scene in ['MenuPrincipal', 'CenaPrincipal', 'Expedicoes']:
    check('Assets/Scenes/' + scene + '.unity' in build, scene + ': registered for build')
check(build.index('Assets/Scenes/MenuPrincipal.unity') < build.index('Assets/Scenes/CenaPrincipal.unity'), 'menu is build entry scene')

destination = (root / 'Assets/Resources/Expeditions/RotaDaMata.asset').read_text(encoding='utf-8')
check('  discoveryDay: 2' in destination, 'unlock on day two')
check('  sceneName: Expedicoes' in destination, 'destination matches registered scene')
for guid in re.findall(r'guid: (\w+)', destination):
    check(guid in guids, 'destination script reference resolves')
for node in (root / 'Assets/Resources/Expeditions/Nodes').glob('*.asset'):
    node_text = node.read_text(encoding='utf-8')
    for guid in re.findall(r'guid: (\w+)', node_text):
        check(guid in guids, node.name + ': resource/script reference resolves')
for night_asset in (root / 'Assets/Resources/NightDirector').rglob('*.asset'):
    for guid in re.findall(r'guid: (\w+)', night_asset.read_text(encoding='utf-8')):
        check(guid in guids, night_asset.name + ': night data reference resolves')
for enemy_asset in (root / 'Assets/Resources/Enemies').glob('*.asset'):
    enemy_text=enemy_asset.read_text(encoding='utf-8')
    for guid in re.findall(r'guid: (\w+)', enemy_text):
        check(guid in guids, enemy_asset.name + ': enemy definition reference resolves')
for name in ['Fodder','Saboteur','Tank','Perseguidor']:
    check((root/'Assets/Resources/Enemies'/(name+'.asset')).exists(), name+': shared enemy definition exists')
check('  cargoCapacity: 100' in destination and '  resourceNodes:' in destination, 'cargo and resource types configured')
for script in (root / 'Assets/Scripts/Expeditions').glob('*.cs'):
    check(Path(str(script) + '.meta').exists(), script.name + ': metadata exists')
print(f'PASS: {checks} expedition scene/asset checks')
