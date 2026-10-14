New-ImageQRCodeSms -Number '+48500000000' `
    -Message 'RESTART SRV-APP-04' -FilePath 'sms.png'
(Get-ImageQRCode -FilePath 'sms.png').Text