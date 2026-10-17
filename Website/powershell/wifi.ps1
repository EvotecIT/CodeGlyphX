New-ImageQRCodeWiFi -SSID 'Evotec-Guest' `
    -Password 'correct-horse-battery' -FilePath 'wifi.png'
(Get-ImageQRCode -FilePath 'wifi.png').Text