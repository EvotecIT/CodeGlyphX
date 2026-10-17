$url = 'https://codeglyphx.com'

New-ImageQRCode -Content $url -FilePath 'branded.png' `
    -ForegroundColor '#4c1d95' -BackgroundColor '#f5f3ff' -PixelSize 12

# Shapes and eye colour; high error correction leaves room for styling.
$style = @{
    ModuleShape     = 'Rounded'
    EyeShape        = 'Circle'
    EyeColor        = '#7c3aed'
    ForegroundColor = '#1e1b4b'
    ErrorCorrection = 'H'
    Size            = 480
}
New-ImageQRCode -Content $url -FilePath 'styled.png' @style

New-ImageQRCode -Content $url -FilePath 'transparent.png' -Transparent -PixelSize 8

# The file extension picks the format: PNG, JPEG, WebP, SVG or PDF.
New-ImageQRCode -Content $url -FilePath 'vector.svg' -ForegroundColor '#4c1d95'

Get-ChildItem *.png | ForEach-Object {
    [pscustomobject]@{
        File = $_.Name
        Size = '{0:N1} KB' -f ($_.Length / 1KB)
        Text = (Get-ImageQRCode -FilePath $_.FullName).Text
    }
} | Format-Table -AutoSize
