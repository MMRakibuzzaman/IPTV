Add-Type -AssemblyName System.Drawing
$bmp = New-Object System.Drawing.Bitmap 456, 456
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.Clear([System.Drawing.Color]::Transparent)
$text = "IPTV"
$font = New-Object System.Drawing.Font("Arial", 85, [System.Drawing.FontStyle]::Bold)
$brush = New-Object System.Drawing.SolidBrush([System.Drawing.ColorTranslator]::FromHtml("#ff4b2b"))
$size = $g.MeasureString($text, $font)
$x = (456 - $size.Width) / 2
$y = (456 - $size.Height) / 2
$g.DrawString($text, $font, $brush, $x, $y)
$bmp.Save("Resources\AppIcon\appiconfg.png", [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose()
$bmp.Dispose()
