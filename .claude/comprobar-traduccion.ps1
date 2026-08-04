# Aviso inmediato al tocar la app: si acabas de escribir un texto a pelo, te lo
# dice AHORA, no dentro de veinte minutos cuando corra el CI.
#
# Solo avisa, nunca bloquea. Un gancho que impide guardar acaba desactivado, y
# un gancho desactivado no protege nada. El que sí corta es el test del CI.
#
# Lo lanza .claude/settings.json cuando se edita algo de src/Ondine.

param([string]$Ruta)

$ErrorActionPreference = 'SilentlyContinue'

if (-not $Ruta -or -not (Test-Path -LiteralPath $Ruta)) { exit 0 }
if ($Ruta -notmatch '\\src\\Ondine\\') { exit 0 }
if ($Ruta -match '\\(obj|bin)\\') { exit 0 }
if ($Ruta -match '\\Localizacion\\') { exit 0 }   # ahí es donde SÍ viven los textos

$avisos = New-Object System.Collections.Generic.List[string]
$lineas = Get-Content -LiteralPath $Ruta -ErrorAction SilentlyContinue
if (-not $lineas) { exit 0 }

if ($Ruta -match '\.xaml$') {
  for ($i = 0; $i -lt $lineas.Count; $i++) {
    foreach ($m in [regex]::Matches($lineas[$i], '\b(Content|Header|Text|ToolTip|Title)\s*=\s*"([^"]*)"')) {
      $v = $m.Groups[2].Value
      if ($v.Length -eq 0 -or $v.StartsWith('{')) { continue }
      if ($v -notmatch '[A-Za-zÁÉÍÓÚáéíóúÑñ]{3,}') { continue }
      $avisos.Add(("  linea {0}: {1}=""{2}""" -f ($i + 1), $m.Groups[1].Value, $v))
    }
  }
}
elseif ($Ruta -match '\.cs$') {
  for ($i = 0; $i -lt $lineas.Count; $i++) {
    $l = $lineas[$i]
    if ($l -match '^\s*(//|///|\*)') { continue }          # comentarios
    # Un literal con acentos o signos de apertura es casi seguro texto que se ve.
    foreach ($m in [regex]::Matches($l, '"[^"\r\n]{4,}"')) {
      if ($m.Value -match '[áéíóúñÁÉÍÓÚÑ¿¡«»]') {
        $avisos.Add(("  linea {0}: {1}" -f ($i + 1), $m.Value))
      }
    }
  }
}

if ($avisos.Count -eq 0) { exit 0 }

Write-Output "TRADUCCION: $([IO.Path]::GetFileName($Ruta)) tiene $($avisos.Count) texto(s) sin pasar por Textos."
$avisos | Select-Object -First 6 | ForEach-Object { Write-Output $_ }
if ($avisos.Count -gt 6) { Write-Output "  ...y $($avisos.Count - 6) mas" }
Write-Output ""
Write-Output "En XAML:  Content=""{i:T MiClave}""     En C#:  Textos.Instancia.MiClave"
Write-Output "La propiedad va en src/Ondine/Localizacion/Textos.<pantalla>.cs."
Write-Output ""
Write-Output "  Mientras desarrollas, solo castellano:"
Write-Output "    public string MiClave => Idioma.Pendiente(""Mi texto"");"
Write-Output ""
Write-Output "  Ya cerrado, los dos idiomas:"
Write-Output "    public string MiClave => Idioma.Elegir(""My text"", ""Mi texto"");"
Write-Output ""
Write-Output "Los pendientes no bloquean nada mientras trabajas, pero el CI no publica"
Write-Output "una version que lleve ninguno."
exit 0
