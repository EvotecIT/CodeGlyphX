$event = @{
    Entry    = 'Patch window'
    Message  = 'Monthly server patching'
    Location = 'Warsaw datacenter'
    From     = [datetime]'2026-11-12 22:00'
    To       = [datetime]'2026-11-13 02:00'
}
New-ImageQRCodeCalendar @event -FilePath 'calendar.png'
(Get-ImageQRCode -FilePath 'calendar.png').Text