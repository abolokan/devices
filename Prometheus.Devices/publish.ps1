param(
    [ValidateSet('cli','worker')]
    [string]$app = 'cli',
    [string[]]$plugins = @('Devices.Camera.AcmeX'),
    [string]$rid = 'win-x64',
    [string]$configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

function Resolve-HostProject {
    param([string]$name)
    switch ($name) {
        'cli' { return "apps/Devices.Host.Cli/Devices.Host.Cli.csproj" }
        'worker' { return "apps/Devices.Host.Worker/Devices.Host.Worker.csproj" }
    }
}

$proj = Resolve-HostProject -name $app
Write-Host "Publishing $proj for $rid ($configuration)" -ForegroundColor Cyan

dotnet publish $proj -c $configuration -r $rid --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=true

$publishDir = Join-Path (Split-Path $proj -Parent) "bin/$configuration/net9.0/$rid/publish"
Write-Host "Publish directory: $publishDir" -ForegroundColor Green

$pluginsRoot = Join-Path $PSScriptRoot 'src/Plugins'
$pluginsDst = Join-Path $publishDir 'plugins'
New-Item -ItemType Directory -Force -Path $pluginsDst | Out-Null

foreach ($p in $plugins) {
    # Найти csproj плагина
    $projPath = Get-ChildItem -Path $pluginsRoot -Recurse -Filter "$p.csproj" | Select-Object -First 1
    if (-not $projPath) {
        Write-Warning "Plugin project '$p' not found."
        continue
    }

    # Собрать плагин в нужной конфигурации
    dotnet build $projPath.FullName -c $configuration | Out-Null

    # Определить результирующую DLL
    $projDir = Split-Path $projPath.FullName -Parent
    $binDir = Join-Path $projDir "bin/$configuration/net9.0"
    $dll = Join-Path $binDir ("$p.dll")
    if (-not (Test-Path $dll)) {
        # fallback: взять первую dll в бин каталоге
        $dll = (Get-ChildItem -Path $binDir -Filter *.dll | Select-Object -First 1).FullName
    }
    if ($dll) {
        Copy-Item $dll -Destination $pluginsDst -Force
        Write-Host "Copied plugin: $(Split-Path $dll -Leaf)" -ForegroundColor Yellow
    }
}

Write-Host "Done." -ForegroundColor Cyan

