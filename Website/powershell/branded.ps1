New-ImageQRCode -Content 'https://codeglyphx.com' `
    -FilePath 'branded.png' -ForegroundColor '#4c1d95' `
    -BackgroundColor '#f5f3ff' -PixelSize 12

New-ImageQRCode -Content 'https://codeglyphx.com' `
    -FilePath 'transparent.png' -Transparent -PixelSize 8

# The file extension picks the format: PNG, JPEG, SVG or PDF.
New-ImageQRCode -Content 'https://codeglyphx.com' `
    -FilePath 'vector.svg' -ForegroundColor '#4c1d95'

Get-ChildItem *.png | ForEach-Object {
    [pscustomobject]@{ File = $_.Name; Text = (Get-ImageQRCode -FilePath $_.Name).Text }
} | Format-Table -AutoSize