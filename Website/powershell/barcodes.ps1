$codes = [ordered]@{
    EAN            = '5901234123457'
    UPCA           = '036000291452'
    ITF14          = '15400141288763'
    Code128        = 'EVO-2026-0042'
    GS1_128        = '(01)09501101530003'
    Code93         = 'CGX-2026'
    DataMatrix     = 'LOT-2026-0042'
    PDF417         = 'SHIPMENT 2026-0042'
    GS1DataBarOmni = '0950110153000'
}

foreach ($type in $codes.Keys) {
    New-ImageBarCode -Type $type -Value $codes[$type] -FilePath "$type.png"
}

# Read each file back. GS1 values come back without the parentheses.
$codes.Keys | ForEach-Object {
    $decoded = Get-ImageBarCode -FilePath "$_.png"
    [pscustomobject]@{ Type = $_; Encoded = $codes[$_]; Decoded = $decoded.Text }
} | Format-Table -AutoSize