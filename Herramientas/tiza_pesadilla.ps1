# Tiza del tablero del 4°B para la Pesadilla (rama de mejoras, 30-oct):
#   tablero_borron.png   en estado normal: un borrón de tiza (algo que alguien escribió y borró)
#   tablero_306.png      en la Pesadilla: «hab. 306» encima del borrón, con la misma letra (Ink Free) del tablero
# Salen en Assets/Resources/Textos/. Uso:  powershell -File Herramientas\tiza_pesadilla.ps1
Add-Type -AssemblyName System.Drawing
$dir = Join-Path $PSScriptRoot "..\Assets\Resources\Textos"
$W = 512; $H = 320
$rnd = New-Object System.Random 306

function Borron($g, $alfa) {
    # pasadas del borrador: manchas blancas suaves, alargadas en horizontal
    for ($i = 0; $i -lt 90; $i++) {
        $a = [int]($alfa * (0.3 + $rnd.NextDouble() * 0.7))
        $b = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb($a, 238, 236, 228))
        $x = 40 + $rnd.NextDouble() * ($W - 160); $y = 50 + $rnd.NextDouble() * ($H - 130)
        $g.FillEllipse($b, [float]$x, [float]$y, [float](60 + $rnd.NextDouble() * 120), [float](14 + $rnd.NextDouble() * 26))
        $b.Dispose()
    }
}

function Guardar($bmp, $nombre) {
    $ruta = Join-Path $dir $nombre
    $bmp.Save($ruta, [System.Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
    Write-Host "listo $ruta"
}

# normal: solo el borrón
$bmp = New-Object System.Drawing.Bitmap $W, $H, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [System.Drawing.Graphics]::FromImage($bmp); $g.SmoothingMode = 'AntiAlias'; $g.Clear([System.Drawing.Color]::Transparent)
Borron $g 26
$g.Dispose(); Guardar $bmp "tablero_borron.png"

# Pesadilla: el borrón y encima, escrito fuerte y torcido, «hab. 306»
$bmp = New-Object System.Drawing.Bitmap $W, $H, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [System.Drawing.Graphics]::FromImage($bmp); $g.SmoothingMode = 'AntiAlias'; $g.TextRenderingHint = 'AntiAliasGridFit'; $g.Clear([System.Drawing.Color]::Transparent)
Borron $g 18
$g.TranslateTransform(256, 160); $g.RotateTransform(-7)
$fuente = New-Object System.Drawing.Font("Ink Free", 92, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$chica = New-Object System.Drawing.Font("Ink Free", 46, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
$fmt = New-Object System.Drawing.StringFormat; $fmt.Alignment = 'Center'; $fmt.LineAlignment = 'Center'
# tiza rojiza, repasada varias veces (un poco corrida en cada pasada)
for ($k = 0; $k -lt 4; $k++) {
    $b = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(150, 214, 92, 80))
    $dx = ($rnd.NextDouble() - 0.5) * 5; $dy = ($rnd.NextDouble() - 0.5) * 5
    $g.DrawString("306", $fuente, $b, [float](30 + $dx), [float](20 + $dy), $fmt)
    $g.DrawString("hab.", $chica, $b, [float](-120 + $dx), [float](-20 + $dy), $fmt)
    $b.Dispose()
}
# raspones: la tiza que no agarró
for ($i = 0; $i -lt 900; $i++) {
    $x = $rnd.Next(0, $W); $y = $rnd.Next(0, $H)
    $c = $bmp.GetPixel($x, $y)
    if ($c.A -gt 100) { $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb([int]($c.A * 0.25), $c.R, $c.G, $c.B)) }
}
$g.Dispose(); Guardar $bmp "tablero_306.png"
