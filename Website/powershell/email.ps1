New-ImageQRCodeEmail -Email 'helpdesk@example.com' `
    -Subject 'Printer on floor 3' -Message 'Paper jam in tray 2' `
    -FilePath 'email.png'
(Get-ImageQRCode -FilePath 'email.png').Text