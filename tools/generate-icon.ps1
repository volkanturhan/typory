# Generates typory's application icon: an emerald->cyan rounded square with three
# white bars that grow longer top to bottom — a short abbreviation "expanding"
# into full text, which is what a text expander does.
#
# Frames are written as uncompressed 32-bit BMP (DIB) entries via GDI+ itself,
# because System.Drawing.Icon / the WinForms NotifyIcon load BMP frames
# reliably, whereas PNG-compressed frames can fail to decode.
#
# Run from anywhere; it writes ../typory/Assets/typory.ico.
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

    # Background rounded square, filled with a diagonal emerald -> cyan gradient.
    $m = [single]($S * 0.06)
    $side = [single]($S - 2 * $m)
    $bg = New-RoundedRect $m $m $side $side ([single]($S * 0.22))
    $emerald = [System.Drawing.Color]::FromArgb(255, 16, 185, 129)   # #10B981
    $cyan = [System.Drawing.Color]::FromArgb(255, 6, 182, 212)       # #06B6D4
    $rect = New-Object System.Drawing.RectangleF(0, 0, $S, $S)
    $grad = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rect, $emerald, $cyan, 45.0)
    $g.FillPath($grad, $bg)

    # Three white bars that grow longer top to bottom (text "expanding").
    $white = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
    $barH = [single]($S * 0.10)
    $lx = [single]($S * 0.28)
    $widths = @([single]($S * 0.20), [single]($S * 0.32), [single]($S * 0.44))
    $top = [single]($S * 0.30)
    $gap = [single]($S * 0.10)
    for ($i = 0; $i -lt 3; $i++) {
        $y = [single]($top + $i * ($barH + $gap))
        $bar = New-RoundedRect $lx $y $widths[$i] $barH ([single]($barH / 2))
        $g.FillPath($white, $bar)
    }

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

$target = Join-Path $PSScriptRoot '..\typory\Assets\typory.ico'
[System.IO.File]::WriteAllBytes($target, $out.ToArray())
$w.Dispose()
Write-Output "Wrote $((Resolve-Path $target).Path) ($((Get-Item $target).Length) bytes)"
