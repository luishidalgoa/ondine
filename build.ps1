<#
    Compila el instalador de "Comprimir vídeos" de punta a punta:
    1) regenera el icono, 2) publica el .exe self-contained, 2b) publica el servidor MCP al lado,
    3) compila el instalador Inno.
    Uso:  pwsh -File build.ps1            (versión leída del .csproj)
          pwsh -File build.ps1 0.2.0      (forzar versión)
    Salida: installer\Output\Ondine-Setup-<version>.exe
#>
param([string]$Version)
$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$csproj = Join-Path $root "src\Ondine\Ondine.csproj"

if (-not $Version) {
    $Version = (Select-Xml -Path $csproj -XPath "//Version").Node.InnerText
}
Write-Host "== Comprimir vídeos · versión $Version ==" -ForegroundColor Cyan

# 1) icono
Write-Host "`n[1/3] Icono..." -ForegroundColor Yellow
pwsh -NoProfile -File (Join-Path $root "make-icon.ps1")
if ($LASTEXITCODE -ne 0) {
    if (Test-Path (Join-Path $root "src\Ondine\Assets\app.ico")) {
        Write-Host "  make-icon falló; uso el app.ico ya versionado." -ForegroundColor DarkYellow
    } else { throw "make-icon falló y no existe app.ico" }
}

# 2) publish self-contained (un solo .exe, sin dependencias del runtime)
Write-Host "`n[2/3] Publicando el ejecutable..." -ForegroundColor Yellow
$publish = Join-Path $root "publish"
if (Test-Path $publish) { Remove-Item -Recurse -Force $publish }
dotnet publish $csproj -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true -p:DebugType=none -p:Version=$Version `
    -p:Official=true -o $publish
if ($LASTEXITCODE -ne 0) { throw "Fallo en dotnet publish" }

# 2b) el servidor MCP, al lado del ejecutable
#
# Va DENTRO del instalador porque el usuario espera que la app instalada lo traiga: instalo
# Ondine, mi agente busca un servidor MCP y no encuentra ninguno. En el .deb, el AppImage y el
# .dmg viaja gratis -comparte el runtime con la interfaz-, pero aqui la app se publica en un
# solo fichero autocontenido, asi que el servidor necesita el suyo: unos 35 MB.
#
# SIN RECORTAR (PublishTrimmed), y no es un olvido: el recortador se lleva los metadatos que
# System.Text.Json necesita por reflexion y el servidor revienta al arrancar, antes de leer una
# sola peticion. Se probo, y el binario recortado -14 MB, la mitad- no sirve.
#
# Se publica a un temporal y se copia solo el .exe, para no meter en la carpeta del instalador
# los .json de configuracion que acompanan a un publish normal.
Write-Host "`n[2b/3] Publicando el servidor MCP..." -ForegroundColor Yellow
$mcpTmp = Join-Path $root "publish-mcp"
if (Test-Path $mcpTmp) { Remove-Item -Recurse -Force $mcpTmp }
dotnet publish (Join-Path $root "src\Ondine.Mcp\Ondine.Mcp.csproj") `
    -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true `
    -p:DebugType=none -p:Version=$Version -o $mcpTmp
if ($LASTEXITCODE -ne 0) { throw "Fallo al publicar el servidor MCP" }

Copy-Item (Join-Path $mcpTmp "ondine-mcp.exe") -Destination $publish -Force
Remove-Item -Recurse -Force $mcpTmp

# Que conteste, no solo que exista. El fallo del recortado ocurria al construir la lista de
# herramientas: un binario roto asi arranca y muere, y sin esto se colaria en el instalador.
$respuesta = '{"jsonrpc":"2.0","id":1,"method":"tools/list"}' |
    & (Join-Path $publish "ondine-mcp.exe") 2>$null
if ($respuesta -notmatch 'ondine_analizar') {
    throw "El servidor MCP no lista sus herramientas: $respuesta"
}
Write-Host "  ondine-mcp.exe listo ($([math]::Round((Get-Item (Join-Path $publish 'ondine-mcp.exe')).Length/1MB,1)) MB)" -ForegroundColor DarkGray

# 3) instalador Inno Setup
Write-Host "`n[3/3] Compilando el instalador..." -ForegroundColor Yellow
$iscc = Get-Command iscc -ErrorAction SilentlyContinue
if (-not $iscc) {
    $iscc = Get-ChildItem `
        "$env:LOCALAPPDATA\Programs\Inno Setup*","C:\Program Files (x86)\Inno Setup*","C:\Program Files\Inno Setup*" `
        -Filter ISCC.exe -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName
} else { $iscc = $iscc.Source }
if (-not $iscc) { throw "No se encuentra ISCC.exe (Inno Setup). Instálalo: winget install JRSoftware.InnoSetup" }

& $iscc "/DMyAppVersion=$Version" (Join-Path $root "installer\ondine.iss")
if ($LASTEXITCODE -ne 0) { throw "Fallo en Inno Setup" }

$out = Join-Path $root "installer\Output\Ondine-Setup-$Version.exe"
Write-Host "`nInstalador listo: $out ($([math]::Round((Get-Item $out).Length/1MB,1)) MB)" -ForegroundColor Green
