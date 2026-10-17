$codes = [ordered]@{
    Ean                       = '5901234123457'
    UpcA                      = '036000291452'
    Itf14                     = '15400141288763'
    Code128                   = 'EVO-2026-0042'
    Gs1Code128                = '(01)09501101530003'
    Code93                    = 'CGX-2026'
    DataMatrix                = 'LOT-2026-0042'
    Pdf417                    = 'SHIPMENT 2026-0042'
    Gs1DataBarOmnidirectional = '0950110153000'
}

foreach ($type in $codes.Keys) {
    New-ImageBarCode -Type $type -Value $codes[$type] -FilePath "$type.png"
}

# Read each file back, looking only for the format that was written.
# GS1 values come back without the parentheses.
$codes.Keys | ForEach-Object {
    [pscustomobject]@{
        Type    = $_
        Encoded = $codes[$_]
        Decoded = (Get-ImageBarCode -FilePath "$_.png" -Type $_).Text
    }
} | Format-Table -AutoSize
