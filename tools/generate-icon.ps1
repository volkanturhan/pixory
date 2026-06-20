# Generates Pixory's application icon: a colourful rounded square (an indigo→pink
# diagonal gradient, evoking "colours") with a white eyedropper on top, the
# universal sign for a screen colour picker.
#
# Frames are written as uncompressed 32-bit BMP (DIB) entries via GDI+ itself,
# because System.Drawing.Icon / the WinForms NotifyIcon load BMP frames
# reliably, whereas PNG-compressed frames can fail to decode.
#
# Run from anywhere; it writes ../Pixory/Assets/Pixory.ico.
Add-Type -AssemblyName System.Drawing

function New-RoundedRect([single]$x, [single]$y, [single]$w, [single]$h, [single]$r) {
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $r * 2
    $path.AddArc($x, $y, $d, $d, 180, 90)
    $path.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $path.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90)
    $path.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $path.CloseFigure()
    return $path
}

function New-IconBitmap([int]$S) {
    $bmp = New-Object System.Drawing.Bitmap($S, $S, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    # Background rounded square, filled with a diagonal indigo -> pink gradient.
    $m = [single]($S * 0.06)
    $side = [single]($S - 2 * $m)
    $bg = New-RoundedRect $m $m $side $side ([single]($S * 0.22))
    $indigo = [System.Drawing.Color]::FromArgb(255, 99, 102, 241)   # #6366F1
    $pink = [System.Drawing.Color]::FromArgb(255, 236, 72, 153)     # #EC4899
    $rect = New-Object System.Drawing.RectangleF(0, 0, $S, $S)
    $grad = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rect, $indigo, $pink, 45.0)
    $g.FillPath($grad, $bg)

    # White eyedropper, drawn vertically then rotated 45 degrees so the tip
    # points to the bottom-left — the classic picker pose.
    $state = $g.Save()
    $g.TranslateTransform([single]($S / 2), [single]($S / 2))
    $g.RotateTransform(45.0)
    $g.TranslateTransform([single](-$S / 2), [single](-$S / 2))

    $white = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
    $cx = [single]($S / 2)

    # Bulb (squeeze top) — a rounded cap.
    $bulbW = [single]($S * 0.22)
    $bulbH = [single]($S * 0.14)
    $bulb = New-RoundedRect ($cx - $bulbW / 2) ([single]($S * 0.22)) $bulbW $bulbH ([single]($bulbH / 2))
    $g.FillPath($white, $bulb)

    # Barrel — the thin body of the dropper.
    $barW = [single]($S * 0.12)
    $barTop = [single]($S * 0.34)
    $barBottom = [single]($S * 0.62)
    $barrel = New-RoundedRect ($cx - $barW / 2) $barTop $barW ($barBottom - $barTop) ([single]($barW * 0.35))
    $g.FillPath($white, $barrel)

    # Tip — a triangle narrowing to a point.
    $tip = New-Object System.Drawing.Drawing2D.GraphicsPath
    $tipPts = @(
        (New-Object System.Drawing.PointF(($cx - $barW / 2), $barBottom)),
        (New-Object System.Drawing.PointF(($cx + $barW / 2), $barBottom)),
        (New-Object System.Drawing.PointF($cx, [single]($S * 0.80)))
    )
    $tip.AddPolygon($tipPts)
    $g.FillPath($white, $tip)

    $g.Restore($state)

    $g.Dispose()
    return $bmp
}

# Returns a complete single-frame .ico (as bytes) for one size, produced by
# GDI+ itself via GetHicon -> Icon.Save, so the pixel data and its directory
# entry are guaranteed mutually consistent; we only repackage them below.
function Get-SingleFrameIco([System.Drawing.Bitmap]$bmp) {
    $hicon = $bmp.GetHicon()
    $icon = [System.Drawing.Icon]::FromHandle($hicon)
    $ms = New-Object System.IO.MemoryStream
    $icon.Save($ms)
    $icon.Dispose()
    $bytes = $ms.ToArray()
    $ms.Dispose()
    return , $bytes
}

$sizes = @(16, 24, 32, 48, 64, 128, 256)

# A typed list, not @() with +=, so the byte[] frames are not flattened.
$singles = New-Object 'System.Collections.Generic.List[byte[]]'
foreach ($s in $sizes) {
    $bmp = New-IconBitmap $s
    $singles.Add((Get-SingleFrameIco $bmp))
    $bmp.Dispose()
}

$out = New-Object System.IO.MemoryStream
$w = New-Object System.IO.BinaryWriter($out)

# ICONDIR header.
$w.Write([uint16]0)
$w.Write([uint16]1)
$w.Write([uint16]$sizes.Count)

# ICONDIRENTRY per frame. Each single-frame .ico already holds a valid 16-byte
# entry at offset 6 describing its frame; we copy it verbatim and only patch the
# byte count and the offset to where the frame sits in the combined file.
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $single = $singles[$i]
    $blobLength = $single.Length - 22

    $entry = New-Object byte[] 16
    [System.Array]::Copy($single, 6, $entry, 0, 16)
    [System.BitConverter]::GetBytes([uint32]$blobLength).CopyTo($entry, 8)   # dwBytesInRes
    [System.BitConverter]::GetBytes([uint32]$offset).CopyTo($entry, 12)      # dwImageOffset

    $w.Write($entry, 0, 16)
    $offset += $blobLength
}

# Frame data, in the same order as the entries above.
foreach ($single in $singles) {
    $w.Write($single, 22, $single.Length - 22)
}
$w.Flush()

$target = Join-Path $PSScriptRoot '..\Pixory\Assets\Pixory.ico'
[System.IO.File]::WriteAllBytes($target, $out.ToArray())
$w.Dispose()
Write-Output "Wrote $((Resolve-Path $target).Path) ($((Get-Item $target).Length) bytes)"
