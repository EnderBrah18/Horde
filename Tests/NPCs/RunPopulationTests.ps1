$ErrorActionPreference = 'Stop'
Push-Location (Join-Path $PSScriptRoot '../..')
try {
    $sdkInfo = (dotnet --list-sdks | Select-Object -Last 1)
    if ($sdkInfo -notmatch '^(\S+) \[(.+)\]') { throw 'Nenhum SDK .NET encontrado.' }
    $sdkVersion = $Matches[1]
    $sdkRoot = $Matches[2]
    $dotnetRoot = Split-Path $sdkRoot
    $targetMajor = $sdkVersion.Split('.')[0]
    $targetFramework = 'net' + $targetMajor + '.0'
    $referencePack = Get-ChildItem (Join-Path $dotnetRoot 'packs/Microsoft.NETCore.App.Ref') -Directory |
        Where-Object { $_.Name.StartsWith($targetMajor + '.') } | Sort-Object { [version]$_.Name } | Select-Object -Last 1
    if ($null -eq $referencePack) { throw 'Reference pack do SDK não encontrado.' }
    New-Item Temp -ItemType Directory -Force | Out-Null
    $compilerArgs = @('-target:exe', '-out:Temp/PopulationTests.dll')
    $compilerArgs += Get-ChildItem (Join-Path $referencePack.FullName "ref/$targetFramework") -Filter *.dll |
        ForEach-Object { '-r:"' + $_.FullName + '"' }
    $compilerArgs += @('Tests/NPCs/PopulationTests.cs', 'Assets/Scripts/NPCs/NpcData.cs',
        'Assets/Scripts/NPCs/NpcRoleData.cs', 'Assets/Scripts/NPCs/VillagePopulation.cs', 'Assets/Scripts/Economy/Wallet.cs')
    Set-Content Temp/PopulationTests.rsp $compilerArgs
    @{runtimeOptions=@{tfm=$targetFramework; framework=@{name='Microsoft.NETCore.App'; version=($targetMajor+'.0.0')}}} |
        ConvertTo-Json -Depth 4 | Set-Content Temp/PopulationTests.runtimeconfig.json
    dotnet (Join-Path $sdkRoot "$sdkVersion/Roslyn/bincore/csc.dll") '@Temp/PopulationTests.rsp'
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar os testes de população.' }
    dotnet Temp/PopulationTests.dll
    if ($LASTEXITCODE -ne 0) { throw 'Falha nos testes de população.' }
}
finally { Pop-Location }
