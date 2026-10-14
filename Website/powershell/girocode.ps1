$payment = @{
    Iban                  = 'DE89370400440532013000'
    Bic                   = 'COBADEFFXXX'
    Name                  = 'Evotec Services'
    Amount                = 99.99
    RemittanceInformation = 'Invoice 2026-0042'
}
New-ImageQRCodeGirocode @payment -FilePath 'girocode.png'
(Get-ImageQRCode -FilePath 'girocode.png').Text