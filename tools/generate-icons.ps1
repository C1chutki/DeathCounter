# One-off generator for the outline icon set (Assets/*.png).
# Uses built-in System.Drawing (no extra dependencies). Renders at 256x256,
# stroke-only, charcoal, round caps/joins, transparent background.
Add-Type -AssemblyName System.Drawing

$ErrorActionPreference = 'Stop'
$assets = Join-Path (Split-Path $PSScriptRoot -Parent) 'Assets'
$size = 256
$dark = [System.Drawing.Color]::FromArgb(255, 31, 31, 31)
$stroke = 18.0

function New-Surface {
    $bmp = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)
    return @{ Bmp = $bmp; G = $g }
}

function New-Pen {
    $p = New-Object System.Drawing.Pen($dark, $stroke)
    $p.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $p.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $p.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    return $p
}

function P([double]$x, [double]$y) { New-Object System.Drawing.PointF($x, $y) }

function Save-Icon($surface, $name) {
    $path = Join-Path $assets $name
    $surface.G.Dispose()
    $surface.Bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $surface.Bmp.Dispose()
    Write-Host "wrote $name"
}

# ---- Edit: pencil (drawn pointing up in local space, rotated -40 deg) ----
$s = New-Surface; $g = $s.G; $pen = New-Pen
$g.TranslateTransform(128, 128)
$g.RotateTransform(-40)
$body = @( (P -40 -96), (P 40 -96), (P 40 60), (P 0 110), (P -40 60) )
$g.DrawPolygon($pen, $body)
$g.DrawLine($pen, (P -40 -56), (P 40 -56))   # eraser band
$g.DrawLine($pen, (P -40 60), (P 40 60))     # wood collar at tip
$g.ResetTransform()
Save-Icon $s 'Edit.png'

# ---- Settings: gear (8 teeth) + center hole ----
$s = New-Surface; $g = $s.G; $pen = New-Pen
$pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Miter   # sharp tooth tips
$pen.MiterLimit = 12.0
$rOuter = 112.0; $rInner = 58.0; $cx = 128.0; $cy = 128.0; $teeth = 6
$pts = @()
# 6 large trapezoidal teeth (60deg period): rise, flat top, fall, valley.
for ($i = 0; $i -lt $teeth; $i++) {
    $base = $i * 60.0
    foreach ($spec in @(@(0.0, $rInner), @(14.0, $rOuter), @(30.0, $rOuter), @(44.0, $rInner))) {
        $ang = ($base + $spec[0]) * [Math]::PI / 180.0
        $rad = $spec[1]
        $pts += P ($cx + $rad * [Math]::Cos($ang)) ($cy + $rad * [Math]::Sin($ang))
    }
}
$g.DrawPolygon($pen, [System.Drawing.PointF[]]$pts)
$g.DrawEllipse($pen, ($cx - 30), ($cy - 30), 60, 60)
Save-Icon $s 'Settings.png'

# ---- Status: gauge / speedometer (top arc + needle + hub) ----
$s = New-Surface; $g = $s.G; $pen = New-Pen
$g.DrawArc($pen, 44, 56, 168, 168, 180, 180)   # top semicircle
$g.DrawLine($pen, (P 128 140), (P 176 86))      # needle
$g.DrawEllipse($pen, 116, 128, 24, 24)          # hub
Save-Icon $s 'Status.png'

# ---- Detection: target / viewfinder scope ----
$s = New-Surface; $g = $s.G; $pen = New-Pen
$g.DrawEllipse($pen, 64, 64, 128, 128)          # ring r=64
$g.DrawLine($pen, (P 128 24), (P 128 64))       # top tick
$g.DrawLine($pen, (P 128 192), (P 128 232))     # bottom tick
$g.DrawLine($pen, (P 24 128), (P 64 128))       # left tick
$g.DrawLine($pen, (P 192 128), (P 232 128))     # right tick
$g.DrawEllipse($pen, 120, 120, 16, 16)          # center dot
Save-Icon $s 'Detection.png'

# ---- Detection_settings: three horizontal sliders ----
$s = New-Surface; $g = $s.G; $pen = New-Pen
$rows = @(@{ Y = 76; Knob = 168 }, @{ Y = 128; Knob = 92 }, @{ Y = 180; Knob = 176 })
foreach ($row in $rows) {
    $g.DrawLine($pen, (P 48 $row.Y), (P 208 $row.Y))
    $g.DrawEllipse($pen, ($row.Knob - 16), ($row.Y - 16), 32, 32)
}
Save-Icon $s 'Detection_settings.png'

# ---- Quick_Settings: lightning bolt ----
$s = New-Surface; $g = $s.G; $pen = New-Pen
$bolt = @( (P 150 32), (P 82 150), (P 122 150), (P 106 224), (P 178 112), (P 134 112) )
$g.DrawPolygon($pen, [System.Drawing.PointF[]]$bolt)
Save-Icon $s 'Quick_Settings.png'

# ---- Quick_Reminders: bell + clapper ----
$s = New-Surface; $g = $s.G; $pen = New-Pen
$path = New-Object System.Drawing.Drawing2D.GraphicsPath
$path.AddBezier((P 80 176), (P 76 118), (P 96 70), (P 128 64))
$path.AddBezier((P 128 64), (P 160 70), (P 180 118), (P 176 176))
$path.AddLine((P 176 176), (P 80 176))
$g.DrawPath($pen, $path)
$path.Dispose()
$g.DrawLine($pen, (P 128 56), (P 128 64))       # top nub
$g.DrawArc($pen, 114, 182, 28, 24, 0, 180)      # clapper (bottom half circle)
Save-Icon $s 'Quick_Reminders.png'

# ---- DashBoard: bar chart + trend arrow ----
$s = New-Surface; $g = $s.G; $pen = New-Pen
$g.DrawLine($pen, (P 44 204), (P 212 204))      # baseline
$g.DrawRectangle($pen, 60, 150, 34, 54)         # bar 1
$g.DrawRectangle($pen, 111, 116, 34, 88)        # bar 2
$g.DrawRectangle($pen, 162, 74, 34, 130)        # bar 3
$g.DrawLine($pen, (P 60 96), (P 196 48))        # trend line
$g.DrawLine($pen, (P 196 48), (P 168 52))       # arrowhead a
$g.DrawLine($pen, (P 196 48), (P 188 76))       # arrowhead b
Save-Icon $s 'DashBoard.png'

# ---- Open_Folder: open folder ----
$s = New-Surface; $g = $s.G; $pen = New-Pen
$back = @( (P 48 104), (P 96 104), (P 112 120), (P 208 120), (P 208 196), (P 48 196) )
$g.DrawPolygon($pen, [System.Drawing.PointF[]]$back)
$front = @( (P 40 196), (P 72 140), (P 232 140), (P 200 196) )
$g.DrawPolygon($pen, [System.Drawing.PointF[]]$front)
Save-Icon $s 'Open_Folder.png'

# ---- Logo: emblem (ring enclosing an upright sword) at native 132x121 ----
$logoW = 132; $logoH = 121
$bmp = New-Object System.Drawing.Bitmap($logoW, $logoH, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)
$lpen = New-Object System.Drawing.Pen($dark, 8.0)
$lpen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$lpen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
$lpen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
$g.DrawEllipse($lpen, 12, 8, 108, 104)            # outer ring
$g.DrawLine($lpen, (P 66 26), (P 66 86))          # blade
$g.DrawLine($lpen, (P 47 66), (P 85 66))          # crossguard
$g.DrawEllipse($lpen, 60, 88, 12, 12)             # pommel
$logoPath = Join-Path $assets 'Logo.png'
$g.Dispose()
$bmp.Save($logoPath, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
Write-Host 'wrote Logo.png'

Write-Host 'done'
