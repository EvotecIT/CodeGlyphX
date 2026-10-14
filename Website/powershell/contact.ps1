$card = @{
    OutputType  = 'VCard4'
    Firstname   = 'Ava'
    Lastname    = 'Stone'
    Org         = 'Evotec'
    OrgTitle    = 'Service desk lead'
    MobilePhone = '+48 500 000 000'
    Email       = 'ava.stone@example.com'
    Website     = 'https://evotec.xyz'
}
New-ImageQRContact @card -FilePath 'contact.png'
(Get-ImageQRCode -FilePath 'contact.png').Text