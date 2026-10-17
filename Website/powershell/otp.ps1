New-ImageQRCodeOtp -Type Totp -SecretBase32 'JBSWY3DPEHPK3PXP' `
    -Label 'ava.stone@example.com' -Issuer 'Evotec' `
    -FilePath 'otp.png'
(Get-ImageQRCode -FilePath 'otp.png').Text