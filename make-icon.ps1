# Genera el icono de Ondine con GDI+ (sin dependencias externas).
#
# La marca es «Corrientes»: tres pasadas horizontales que van de onda a recta —lo que entra
# revuelto sale ordenado— sobre retícula 64 y grosor 6, con extremos redondos. Es un trazo
# abierto, SIN contenedor: nada de squircle con fondo, que era el icono anterior y contaba que
# esto es un compresor de vídeo, no lo que la app hace hoy.
#
# La geometría vive en docs\marca\*.svg y este script la reproduce. Si cambia el diseño, cambia
# ahí primero y refleja el cambio aquí.
#
# A 16 px va un dibujo APARTE: retícula de 16, grosor 2, extremos rectos y vértices enteros. Una
# Bézier a ese tamaño se difumina en gris y se pierde la progresión onda→recta, que es lo único
# que sostiene el concepto; el zigzag la conserva nítida.
#
# Salidas: src\Ondine\Assets\app.ico (multirresolución 16..256),
#          src\Ondine\Assets\app-256.png y docs\icon.png (README/repo).
Add-Type -AssemblyName System.Drawing
$ErrorActionPreference = "Stop"

# Un solo color para todos los tamaños. El diseño propone #B5ABFC para <=32 px sobre fondo
# oscuro, pero un .ico tiene que sobrevivir TAMBIÉN a una barra de tareas clara, y ahí ese tono
# se lava (~2:1 sobre blanco). #968AE0 aguanta las dos. Las variantes exactas quedan en los SVG.
$Marca = [System.Drawing.Color]::FromArgb(255, 150, 138, 224)   # #968AE0 (Theme.xaml: c.accent)

function Render([int]$S) {
    $bmp = New-Object System.Drawing.Bitmap($S, $S)
    $g = [System.Drawing.Graphics]::FromImage($bmp)

    if ($S -le 20) {
        # --- dibujo de píxel entero, para bandeja y favicon ---
        $g.SmoothingMode = 'None'
        $k = [float]($S / 16.0)
        $pen = New-Object System.Drawing.Pen($Marca, [float](2 * $k))
        $pen.StartCap = 'Flat'; $pen.EndCap = 'Flat'; $pen.LineJoin = 'Miter'
        # Los arrays van tipados: con Object[] PowerShell no acierta con la sobrecarga.
        $p = { param([float]$x, [float]$y) New-Object System.Drawing.PointF(($x * $k), ($y * $k)) }
        $g.DrawLines($pen, [System.Drawing.PointF[]]@((& $p 2 4), (& $p 5 2), (& $p 8 4), (& $p 11 2), (& $p 14 4)))
        $g.DrawLines($pen, [System.Drawing.PointF[]]@((& $p 2 9), (& $p 5 8), (& $p 8 9), (& $p 11 8), (& $p 14 9)))
        $g.DrawLine($pen, (& $p 2 13), (& $p 14 13))
        $pen.Dispose(); $g.Dispose()
        return $bmp
    }

    # --- la marca normal ---
    $g.SmoothingMode   = 'AntiAlias'
    $g.PixelOffsetMode = 'HighQuality'
    $k = [float]($S / 64.0)
    $pen = New-Object System.Drawing.Pen($Marca, [float](6 * $k))
    # Extremos y uniones redondos: es lo que hace que el trazo parezca dibujado y no cortado.
    $pen.StartCap = 'Round'; $pen.EndCap = 'Round'; $pen.LineJoin = 'Round'

    $q = { param([float]$x, [float]$y) New-Object System.Drawing.PointF(($x * $k), ($y * $k)) }

    # M7 17 C 15.5 9 26.5 25 35 17 C 43.5 9 48.5 25 57 17   — la onda
    $g.DrawBeziers($pen, [System.Drawing.PointF[]]@((& $q 7 17), (& $q 15.5 9), (& $q 26.5 25), (& $q 35 17),
                                                    (& $q 43.5 9), (& $q 48.5 25), (& $q 57 17)))
    # M7 32 C 16 28 26 36 35 32 C 44 28 48 36 57 32   — media onda, exagerada a propósito
    $g.DrawBeziers($pen, [System.Drawing.PointF[]]@((& $q 7 32), (& $q 16 28), (& $q 26 36), (& $q 35 32),
                                                    (& $q 44 28), (& $q 48 36), (& $q 57 32)))
    # M7 47 L 57 47   — la recta
    $g.DrawLine($pen, (& $q 7 47), (& $q 57 47))

    $pen.Dispose(); $g.Dispose()
    return $bmp
}

# --- .ico multirresolución ---
# 16 y 20 salen del dibujo de píxel entero; de 24 en adelante, de la marca normal.
$sizes = @(16, 20, 24, 32, 48, 64, 128, 256)
$pngs = @()
foreach ($s in $sizes) {
    $bmp = Render $s
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngs += , ($ms.ToArray())
    $bmp.Dispose(); $ms.Dispose()
}

$out = Join-Path $PSScriptRoot "src\Ondine\Assets\app.ico"
$fs = [System.IO.File]::Create($out)
$bw = New-Object System.IO.BinaryWriter($fs)
$bw.Write([uint16]0); $bw.Write([uint16]1); $bw.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s = $sizes[$i]; $len = $pngs[$i].Length
    # 0 significa 256 en el formato ICO: el campo es de un solo byte.
    $bw.Write([byte]$(if ($s -ge 256) { 0 } else { $s }))
    $bw.Write([byte]$(if ($s -ge 256) { 0 } else { $s }))
    $bw.Write([byte]0); $bw.Write([byte]0)
    $bw.Write([uint16]1); $bw.Write([uint16]32)
    $bw.Write([uint32]$len); $bw.Write([uint32]$offset)
    $offset += $len
}
foreach ($p in $pngs) { $bw.Write($p) }
$bw.Flush(); $fs.Close()

# PNG grande: vista previa interna y portada del README
$big = Render 256
$big.Save((Join-Path $PSScriptRoot "src\Ondine\Assets\app-256.png"), [System.Drawing.Imaging.ImageFormat]::Png)
$docs = Join-Path $PSScriptRoot "docs"
if (-not (Test-Path $docs)) { New-Item -ItemType Directory $docs | Out-Null }
$big.Save((Join-Path $docs "icon.png"), [System.Drawing.Imaging.ImageFormat]::Png)
$big.Dispose()

Write-Host "Icono generado: $out ($([math]::Round((Get-Item $out).Length/1KB,1)) KB · $($sizes -join '/') px)"
