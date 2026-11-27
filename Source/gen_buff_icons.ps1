# Draws AposMod buff / HUD icons: white on transparent, 128x128 (tinted in game by icon_color)
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

# Frenzy (+15% attack speed): a big sword mid-swing with the swing's motion arcs behind it
$icons['ui_apos_frenzy'] = {
    param($g)
    # motion arcs around the hilt, on the side the blade came from
    foreach ($a in @(@(98, 9, 255), @(78, 8, 170), @(58, 7, 100))) {
        $pen = New-Object Drawing.Pen([Drawing.Color]::FromArgb($a[2], 255, 255, 255), $a[1])
        $pen.StartCap = 'Round'; $pen.EndCap = 'Round'
        $r = $a[0]
        $g.DrawArc($pen, (30 - $r), (110 - $r), (2 * $r), (2 * $r), 262, 42)
    }
    # the sword: blade from the hilt (30,110) to the tip (116,16), guard across, grip
    $g.FillPolygon($white, (P @(40,92, 106,22, 118,12, 110,30, 48,100)))
    $g.FillPolygon($white, (P @(30,80, 60,110, 52,118, 22,88)))
    $g.FillPolygon($white, (P @(34,102, 42,110, 18,126, 10,118)))
}

# Adrenaline: a sprinter leaning forward, with speed lines behind
$icons['ui_apos_adrenaline'] = {
    param($g)
    $g.FillEllipse($white, 76, 6, 26, 26)
    $p = Pen 16
    $g.DrawLine($p, 80, 42, 60, 74)                                         # torso
    $p = Pen 11
    $g.DrawLines($p, (P @(76,46, 96,60, 112,48)))                           # front arm
    $g.DrawLines($p, (P @(72,46, 52,54, 40,42)))                            # back arm
    $g.DrawLines($p, (P @(60,76, 84,88, 80,118)))                           # front leg
    $g.DrawLines($p, (P @(58,78, 44,100, 18,104)))                          # back leg
    $p = Pen 6
    $g.DrawLine($p, 4, 34, 34, 34); $g.DrawLine($p, 2, 56, 28, 56); $g.DrawLine($p, 8, 78, 30, 78)
}

# Movement speed: a running shoe with speed lines
$icons['ui_apos_speed'] = {
    param($g)
    $shoe = New-Object Drawing.Drawing2D.GraphicsPath
    $shoe.AddLines((P @(40,44, 62,44, 70,62, 100,72, 118,82, 120,96, 40,96)))
    $shoe.CloseFigure()
    $g.FillPath($white, $shoe)
    $g.FillRectangle($white, 38, 100, 84, 10)
    $p = Pen 7
    $g.DrawLine($p, 6, 52, 30, 52); $g.DrawLine($p, 2, 70, 30, 70); $g.DrawLine($p, 8, 88, 30, 88)
}

# Jackpot (Mother Lode 5, harvest x5): a faceted gem with sparkles
$icons['ui_apos_jackpot'] = {
    param($g)
    $g.FillPolygon($white, (P @(30,44, 52,22, 92,22, 114,44, 72,110)))
    $g.CompositingMode = 'SourceCopy'
    $clear = New-Object Drawing.SolidBrush([Drawing.Color]::Transparent)
    $edge = New-Object Drawing.Pen([Drawing.Color]::Transparent, 5)
    $g.DrawLine($edge, 30, 46, 114, 46)
    $g.DrawLine($edge, 52, 24, 62, 46); $g.DrawLine($edge, 92, 24, 82, 46)
    $g.DrawLine($edge, 62, 46, 72, 106); $g.DrawLine($edge, 82, 46, 72, 106)
    $g.CompositingMode = 'SourceOver'
    # sparkles: four-point stars
    foreach ($s in @(@(18, 18, 12), @(112, 92, 10), @(116, 10, 7))) {
        $x = $s[0]; $y = $s[1]; $r = $s[2]; $q = $r * 0.28
        $g.FillPolygon($white, (P @($x,($y-$r), ($x+$q),($y-$q), ($x+$r),$y, ($x+$q),($y+$q), $x,($y+$r), ($x-$q),($y+$q), ($x-$r),$y, ($x-$q),($y-$q))))
    }
}

# Free Rocket (legendary Big Boom): a rocket flying up-right with a flame trail
$icons['ui_apos_rocket'] = {
    param($g)
    $st = $g.Save()
    $g.TranslateTransform(64, 64); $g.ScaleTransform(1.3, 1.3); $g.RotateTransform(45)
    # body, nose cone, fins (drawn pointing up, then turned 45 degrees)
    $g.FillRectangle($white, -12, -30, 24, 50)
    $g.FillPolygon($white, (P @(-12,-30, 0,-56, 12,-30)))
    $g.FillPolygon($white, (P @(-12,4, -26,24, -12,20)))
    $g.FillPolygon($white, (P @(12,4, 26,24, 12,20)))
    # a window
    $g.CompositingMode = 'SourceCopy'
    $g.FillEllipse((New-Object Drawing.SolidBrush([Drawing.Color]::Transparent)), -6, -22, 12, 12)
    $g.CompositingMode = 'SourceOver'
    # flame
    $g.FillPolygon((New-Object Drawing.SolidBrush([Drawing.Color]::FromArgb(200, 255, 255, 255))), (P @(-9,22, 0,50, 9,22)))
    $g.FillPolygon($white, (P @(-5,22, 0,36, 5,22)))
    $g.Restore($st)
}

# Corroded Armor (an Acidic elite's hit): a chest plate split by a jagged crack, acid dripping from it
$icons['ui_apos_acidarmor'] = {
    param($g)
    $g.FillPolygon($white, (P @(34,8, 50,8, 56,20, 72,20, 78,8, 94,8, 118,24, 108,52, 98,48, 98,96, 30,96, 30,48, 20,52, 10,24)))
    $g.CompositingMode = 'SourceCopy'
    $clear = New-Object Drawing.SolidBrush([Drawing.Color]::Transparent)
    $g.FillPolygon($clear, (P @(70,20, 58,40, 72,54, 56,72, 68,84, 60,98, 70,98, 80,84, 68,72, 84,54, 70,40, 78,20)))
    $g.CompositingMode = 'SourceOver'
    # drops under the plate
    foreach ($d in @(@(44,104,8), @(86,108,10), @(64,116,7))) {
        $x = $d[0]; $y = $d[1]; $r = $d[2]
        $g.FillEllipse($white, ($x - $r), ($y - $r / 2), (2 * $r), (2 * $r))
        $g.FillPolygon($white, (P @(($x - $r * 0.8),($y + $r * 0.2), $x,($y - $r * 1.6), ($x + $r * 0.8),($y + $r * 0.2))))
    }
}

# a pistol facing right (shared by the two fire-rate icons): slide, trigger guard, grip
function Draw-Pistol($g, [float]$dx, [float]$dy, [float]$s) {
    $pts = @(4,0, 64,0, 64,14, 30,14, 26,22, 30,46, 14,46, 12,22, 4,14)
    $q = @(); for ($i = 0; $i -lt $pts.Length; $i += 2) { $q += $dx + $pts[$i] * $s; $q += $dy + $pts[$i + 1] * $s }
    $g.FillPolygon($white, (P $q))
}

# Gunfighter (+15% fire rate after a headshot): a pistol rushing forward, speed lines behind it
$icons['ui_apos_gunfighter'] = {
    param($g)
    Draw-Pistol $g 34 38 1.15
    $p = Pen 7
    $g.DrawLine($p, 4, 42, 28, 42); $g.DrawLine($p, 10, 60, 30, 60); $g.DrawLine($p, 4, 78, 26, 78)
    # a muzzle flash
    $g.FillPolygon($white, (P @(126,46, 116,36, 116,44, 110,46, 116,48, 116,56)))
}

# Hot Barrel (+5% fire rate per hit, up to +100%): a pistol with heat waves over the barrel and a burst of bullets ahead
$icons['ui_apos_hotbarrel'] = {
    param($g)
    Draw-Pistol $g 6 52 1.2
    $p = Pen 5
    foreach ($x in @(30, 50, 70)) {
        $path = New-Object Drawing.Drawing2D.GraphicsPath
        $path.AddBezier($x, 46, ($x - 8), 36, ($x + 8), 28, $x, 18)
        $g.DrawPath($p, $path)
    }
    foreach ($b in @(@(96, 62, 255), @(110, 62, 190), @(122, 62, 120))) {
        $br = New-Object Drawing.SolidBrush([Drawing.Color]::FromArgb($b[2], 255, 255, 255))
        $g.FillEllipse($br, ($b[0] - 5), ($b[1] - 5), 10, 10)
    }
}

# Shattered (shotgun A5, on a zombie): the same cracked chest plate as Corroded Armor, tinted orange in the game
$icons['ui_apos_shattered'] = $icons['ui_apos_acidarmor']

# Suppression (MG trait, on a zombie): "duck and cover" seen from the front - a crouched body, the head pulled down,
# both arms in a thick arch over it; bold shapes so it still reads at 20 px
$icons['ui_apos_suppressed'] = {
    param($g)
    $g.FillEllipse($white, 26, 70, 76, 46)                                  # crouched body and knees
    $g.FillRectangle($white, 30, 106, 26, 14); $g.FillRectangle($white, 72, 106, 26, 14)   # feet
    $arm = New-Object Drawing.Pen([Drawing.Color]::White, 18)
    $arm.StartCap = 'Round'; $arm.EndCap = 'Round'
    $g.DrawArc($arm, 22, 22, 84, 84, 180, 180)                              # arms over the head
    $g.CompositingMode = 'SourceCopy'
    $clear = New-Object Drawing.SolidBrush([Drawing.Color]::Transparent)
    $g.FillEllipse($clear, 42, 44, 44, 44)                                  # a gap around the head
    $g.FillRectangle($clear, 61, 10, 6, 26)                                  # the two arms meet over the head
    $g.CompositingMode = 'SourceOver'
    $g.FillEllipse($white, 46, 16, 16, 16); $g.FillEllipse($white, 66, 16, 16, 16)   # hands clasped on top
    $g.FillEllipse($white, 48, 50, 32, 32)                                  # the head, pulled down
    # a few incoming shots
    $p = Pen 6
    $g.DrawLine($p, 6, 18, 18, 30); $g.DrawLine($p, 122, 18, 110, 30); $g.DrawLine($p, 64, 2, 64, 12)
}

# Hold the Line (MG A6, crouch / stand still): a machine-gun turret on a tripod
$icons['ui_apos_holdline'] = {
    param($g)
    $p = Pen 9
    $g.DrawLine($p, 58, 78, 30, 120); $g.DrawLine($p, 64, 78, 64, 122); $g.DrawLine($p, 70, 78, 98, 120)   # tripod
    $g.FillRectangle($white, 52, 66, 24, 16)                                # swivel
    $g.FillPolygon($white, (P @(20,40, 84,40, 92,48, 92,66, 20,66)))        # gun body
    $g.FillRectangle($white, 90, 48, 34, 9)                                 # barrel
    $g.FillRectangle($white, 116, 44, 8, 17)                                # muzzle
    $g.FillRectangle($white, 34, 66, 20, 18)                                # ammo box
    $g.FillPolygon($white, (P @(4,46, 20,42, 20,62, 4,58)))                 # stock
    $g.CompositingMode = 'SourceCopy'
    $clear = New-Object Drawing.SolidBrush([Drawing.Color]::Transparent)
    foreach ($x in @(30, 44, 58, 72)) { $g.FillRectangle($clear, $x, 46, 6, 14) }   # cooling slots
    $g.CompositingMode = 'SourceOver'
}

# Full Draw (bow B1): an arrow flying right, a damage burst at its tip
$icons['ui_apos_fulldraw'] = {
    param($g)
    $p = Pen 9
    $g.DrawLine($p, 14, 64, 82, 64)
    $g.FillPolygon($white, (P @(4,48, 24,64, 4,80, 14,64)))                  # fletching
    $g.FillPolygon($white, (P @(76,48, 100,64, 76,80)))                     # head
    $pts = @(); $n = 8
    for ($i = 0; $i -lt $n * 2; $i++) {
        $an = [Math]::PI * $i / $n
        $r = if ($i % 2 -eq 0) { 24 } else { 11 }
        $pts += New-Object Drawing.PointF((106 + $r * [Math]::Cos($an)), (64 + $r * [Math]::Sin($an)))
    }
    $g.FillPolygon($white, $pts)
}

# Firing Squad ready (the next hit deals 1000%): a reticle with a blast in its centre
$icons['ui_apos_squad'] = {
    param($g)
    $p = Pen 8
    $g.DrawEllipse($p, 18, 18, 92, 92)
    $g.DrawLine($p, 64, 2, 64, 26); $g.DrawLine($p, 64, 102, 64, 126); $g.DrawLine($p, 2, 64, 26, 64); $g.DrawLine($p, 102, 64, 126, 64)
    $pts = @(); $n = 8
    for ($i = 0; $i -lt $n * 2; $i++) {
        $an = [Math]::PI * $i / $n - [Math]::PI / 2
        $r = if ($i % 2 -eq 0) { 30 } else { 13 }
        $pts += New-Object Drawing.PointF((64 + $r * [Math]::Cos($an)), (64 + $r * [Math]::Sin($an)))
    }
    $g.FillPolygon($white, $pts)
}

# Critical hit: a jagged burst with an exclamation mark cut out
$icons['ui_apos_crit'] = {
    param($g)
    $pts = @(); $n = 12
    for ($i = 0; $i -lt $n * 2; $i++) {
        $a = [Math]::PI * $i / $n - [Math]::PI / 2
        $r = if ($i % 2 -eq 0) { if ($i % 4 -eq 0) { 62 } else { 52 } } else { 34 }
        $pts += New-Object Drawing.PointF((64 + $r * [Math]::Cos($a)), (64 + $r * [Math]::Sin($a)))
    }
    $g.FillPolygon($white, $pts)
    $g.CompositingMode = 'SourceCopy'
    $clear = New-Object Drawing.SolidBrush([Drawing.Color]::Transparent)
    $g.FillPolygon($clear, (P @(56,30, 72,30, 68,74, 60,74)))
    $g.FillEllipse($clear, 57, 82, 14, 14)
    $g.CompositingMode = 'SourceOver'
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

$colors = @{ ui_apos_frenzy = '255,180,0'; ui_apos_adrenaline = '0,220,120'; ui_apos_crit = '255,210,77'; ui_apos_jackpot = '255,210,0'; ui_apos_rocket = '255,180,0'; ui_apos_acidarmor = '120,255,60'; ui_apos_gunfighter = '255,210,77'; ui_apos_hotbarrel = '255,150,50'; ui_apos_shattered = '255,153,51'; ui_apos_suppressed = '115,191,255'; ui_apos_holdline = '255,210,77'; ui_apos_fulldraw = '255,210,77'; ui_apos_squad = '255,210,77'; ui_apos_speed = '90,230,140'; ui_apos_evasion = '0,200,255'; ui_apos_haste = '255,210,77'; ui_apos_lungs = '150,150,150' }
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

