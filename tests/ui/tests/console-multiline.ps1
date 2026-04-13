Clear-Host

$colors = @(
"Black";
"Blue";
"Cyan";
"DarkBlue";
"DarkGray";
"DarkGreen";
"DarkMagenta";
"DarkRed";
"DarkYellow";
"Gray";
"Green";
"Magenta";
"Red";
"White";
"Yellow")

foreach ($color in $colors){
    Write-Host "test test" -fore $color  -BackgroundColor $color -NoNewline
    Write-Host "test test" -BackgroundColor $color -NoNewline
    Write-Host "test test" -ForegroundColor $color
}

Write-Host "Done!"
