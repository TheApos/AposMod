# Draws AposMod buff icons: white on transparent, 128x128 (tinted in game by icon_color)
param([string]$Out, [string]$Preview)
Add-Type -AssemblyName System.Drawing
$S = 128

function New-Canvas {
    $bmp = New-Object Drawing.Bitmap $S, $S, ([Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'; $g.PixelOffsetMode = 'HighQuality'
    $g.Clear([Drawing.Color]::Transparent)
    return @($bmp, $g)
}
function P([float[]]$xy) { $pts = @(); for ($i = 0; $i -lt $xy.Length; $i += 2) { $pts += New-Object Drawing.PointF($xy[$i], $xy[$i + 1]) }; return ,$pts }
$white = [Drawing.Brushes]::White
function Pen([float]$w) { $p = New-Object Drawing.Pen([Drawing.Color]::White, $w); $p.StartCap = 'Round'; $p.EndCap = 'Round'; $p.LineJoin = 'Round'; return $p }

$icons = [ordered]@{}

# Frenzy: three claw slashes
$icons['ui_apos_frenzy'] = {
    param($g)
    for ($i = 0; $i -lt 3; $i++) {
        $o = $i * 24 - 6
        $path = New-Object Drawing.Drawing2D.GraphicsPath
        $path.AddBezier(18 + $o, 104, 34 + $o, 74, 54 + $o, 40, 82 + $o, 14)
        $path.AddBezier(82 + $o, 14, 64 + $o, 44, 46 + $o, 78, 30 + $o, 108)
        $path.CloseFigure()
        $g.FillPath($white, $path)
    }
}

# Adrenaline: lightning bolt with speed lines
$icons['ui_apos_adrenaline'] = {
    param($g)
    $g.FillPolygon($white, (P @(74,6, 34,70, 62,70, 46,122, 98,48, 68,48, 88,6)))
    $p = Pen 7
    $g.DrawLine($p, 8, 40, 30, 40); $g.DrawLine($p, 4, 62, 26, 62); $g.DrawLine($p, 10, 84, 30, 84)
}

# Evasion: a dodging silhouette with two fading afterimages
$icons['ui_apos_evasion'] = {
    param($g)
    $shapes = @(@(0, 70), @(-22, 140), @(-44, 255))
    foreach ($s in ($shapes | Sort-Object { $_[1] })) {
        $dx = 22 + $s[0]; $b = New-Object Drawing.SolidBrush([Drawing.Color]::FromArgb($s[1], 255, 255, 255))
        $g.FillEllipse($b, 62 + $dx, 10, 24, 24)
        $g.FillPolygon($b, (P @((52+$dx),38, (84+$dx),40, (78+$dx),78, (96+$dx),112, (80+$dx),118, (62+$dx),86, (46+$dx),118, (30+$dx),112, (50+$dx),76)))
    }
}

# Mastery Haste: hourglass with speed lines
$icons['ui_apos_haste'] = {
    param($g)
    $g.FillRectangle($white, 40, 10, 72, 10); $g.FillRectangle($white, 40, 108, 72, 10)
    $p = Pen 7
    $g.DrawPolygon($p, (P @(48,22, 104,22, 82,64, 104,106, 48,106, 70,64)))
    $g.FillPolygon($white, (P @(58,30, 94,30, 76,58)))
    $g.FillPolygon($white, (P @(76,74, 98,104, 54,104)))
    $g.DrawLine($p, 6, 46, 30, 46); $g.DrawLine($p, 2, 64, 30, 64); $g.DrawLine($p, 6, 82, 30, 82)
}

# Second Lungs: a pair of lungs with the windpipe
$icons['ui_apos_lungs'] = {
    param($g)
    $p = Pen 9
    $g.DrawLine($p, 64, 8, 64, 50); $g.DrawLine($p, 64, 50, 50, 62); $g.DrawLine($p, 64, 50, 78, 62)
    $l = New-Object Drawing.Drawing2D.GraphicsPath
    $l.AddBezier(54, 40, 30, 30, 10, 70, 14, 104); $l.AddBezier(14, 104, 18, 122, 46, 122, 56, 106); $l.AddLine(56, 106, 56, 70); $l.CloseFigure()
    $g.FillPath($white, $l)
    $m = New-Object Drawing.Drawing2D.Matrix(-1, 0, 0, 1, 128, 0); $r = $l.Clone(); $r.Transform($m); $g.FillPath($white, $r)
}

$colors = @{ ui_apos_frenzy = '255,180,0'; ui_apos_adrenaline = '0,220,120'; ui_apos_evasion = '0,200,255'; ui_apos_haste = '255,210,77'; ui_apos_lungs = '150,150,150' }
$sheet = New-Object Drawing.Bitmap (($S + 16) * $icons.Count + 16), ($S + 32)
$sg = [Drawing.Graphics]::FromImage($sheet); $sg.Clear([Drawing.Color]::FromArgb(30, 30, 30))
$x = 16
foreach ($name in $icons.Keys) {
    $c = New-Canvas; $bmp = $c[0]; $g = $c[1]
    & $icons[$name] $g
    $g.Dispose()
    if ($Out) { $bmp.Save((Join-Path $Out "$name.png"), [Drawing.Imaging.ImageFormat]::Png) }
    # preview tinted like the game does
    $rgb = $colors[$name] -split ','
    $cm = New-Object Drawing.Imaging.ColorMatrix
    $cm.Matrix00 = [float]$rgb[0] / 255; $cm.Matrix11 = [float]$rgb[1] / 255; $cm.Matrix22 = [float]$rgb[2] / 255
    $ia = New-Object Drawing.Imaging.ImageAttributes; $ia.SetColorMatrix($cm)
    $sg.DrawImage($bmp, (New-Object Drawing.Rectangle($x, 16, $S, $S)), 0, 0, $S, $S, 'Pixel', $ia)
    $bmp.Dispose(); $x += $S + 16
}
$sheet.Save($Preview, [Drawing.Imaging.ImageFormat]::Png)
"ok"

