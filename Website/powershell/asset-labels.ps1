# Asset tags to label. Replace them with Get-ADComputer, Import-Csv or any list you have.
$tags = 'EVO-LAP-0142', 'EVO-LAP-0177', 'EVO-DSK-0031', 'EVO-MON-0219'

foreach ($tag in $tags) {
    New-ImageQRCode -Content "https://assets.example.com/$tag" `
        -FilePath "$tag.qr.png" -ForegroundColor '#1e1b4b'
    New-ImageBarCode -Type Code128 -Value $tag -FilePath "$tag.barcode.png"
}

# One printable sheet with every QR code.
New-ImageMosaic -FilePaths ($tags | ForEach-Object { "$_.qr.png" }) `
    -OutputPath 'labels.png' -Columns 4 -Width 220 -Height 220

# Read every label back before it goes to the printer.
$tags | ForEach-Object {
    [pscustomobject]@{
        Tag     = $_
        QRCode  = (Get-ImageQRCode -FilePath "$_.qr.png").Text
        Barcode = (Get-ImageBarCode -FilePath "$_.barcode.png").Text
    }
} | Format-Table -AutoSize