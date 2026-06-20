param()
$projectDir='C:\Users\liman\Documents\FoodDeliveryProject\backend\FoodDeliveryyy\FoodDeliveryyy'
Set-Location $projectDir
$baseRoot='http://localhost:5063'; $baseUrl="$baseRoot/api"
$started=$false; $proc=$null
function Up { try { Invoke-WebRequest "$baseRoot/" -TimeoutSec 2 | Out-Null; $true } catch { [bool]$_.Exception.Response } }
if(-not (Up)){ $proc=Start-Process dotnet -ArgumentList 'run --urls http://localhost:5063' -WorkingDirectory $projectDir -PassThru -RedirectStandardOutput "$env:TEMP\fd_out.log" -RedirectStandardError "$env:TEMP\fd_err.log"; $started=$true; for($i=0;$i -lt 50 -and -not (Up);$i++){} }
function Call($m,$p,$t,$b){ $h=@{}; if($t){$h.Authorization="Bearer $t"}; $u="$baseUrl$p"; $raw='';$s=0; try{ if($null -ne $b){$j=$b|ConvertTo-Json -Compress -Depth 8; $r=Invoke-WebRequest $u -Method $m -Headers $h -ContentType 'application/json' -Body $j}else{$r=Invoke-WebRequest $u -Method $m -Headers $h}; $s=[int]$r.StatusCode; $raw=[string]$r.Content } catch { if($_.Exception.Response){$s=[int]$_.Exception.Response.StatusCode; $sr=New-Object IO.StreamReader($_.Exception.Response.GetResponseStream()); $raw=$sr.ReadToEnd(); $sr.Close()} else {$raw=$_.Exception.Message} }; $c=$raw; try{$c=($raw|ConvertFrom-Json|ConvertTo-Json -Compress -Depth 8)}catch{}; if($c.Length -gt 160){$c=$c.Substring(0,160)+'...'}; [pscustomobject]@{Status=$s;Raw=$raw;Body=$c} }
function Tok($raw){ try{$o=$raw|ConvertFrom-Json; if($o.token){$o.token}elseif($o.accessToken){$o.accessToken}elseif($o.data.token){$o.data.token}}catch{} }
$R=@(); function Add($n,$e,$a,$ok,$b){ $script:R+=[pscustomobject]@{Test=$n;Expected=$e;Actual=$a;Pass=($(if($ok){'PASS'}else{'FAIL'}));Body=$b} }
$a=Call 'POST' '/Auth/login' $null @{UsernameOrEmail='admin';Password='Admin@1234'}; $at=Tok $a.Raw; Add 'Admin login' '200' $a.Status ($a.Status -eq 200 -and $at) $a.Body
$o=Call 'POST' '/Auth/login' $null @{UsernameOrEmail='sushico';Password='Merchant@1234'}; $ot=Tok $o.Raw; Add 'Merchant login' '200' $o.Status ($o.Status -eq 200 -and $ot) $o.Body
$x=Call 'GET' '/Dashboard/merchant' $ot $null; Add 'Merchant /Dashboard/merchant' '200' $x.Status ($x.Status -eq 200) $x.Body
$addr=Call 'GET' '/Restaurants/1/addresses' $ot $null; Add 'Merchant /Restaurants/1/addresses' '200' $addr.Status ($addr.Status -eq 200) $addr.Body
$branchId=1; $rid=1; if($addr.Status -eq 200){ try{$obj=$addr.Raw|ConvertFrom-Json; $f=$null; if($obj.data){$f=@($obj.data)[0]}elseif($obj.items){$f=@($obj.items)[0]}else{$f=@($obj)[0]}; if($f.restaurantAddressId){$branchId=[int]$f.restaurantAddressId}elseif($f.id){$branchId=[int]$f.id}; if($f.restaurantId){$rid=[int]$f.restaurantId}}catch{} }
$r=Call 'POST' '/Auth/register' $null @{Username='bm_scope_test';Email='bm_scope_test@example.com';Password='Branch@1234';ConfirmPassword='Branch@1234'}; Add 'Register bm_scope_test' '200/201/400/409' $r.Status ($r.Status -in 200,201,400,409) $r.Body
$asg=Call 'POST' '/Auth/admin/assign-branch-manager' $at @{UsernameOrEmail='bm_scope_test';RestaurantAddressId=$branchId}; Add 'Assign branch manager' '200' $asg.Status ($asg.Status -eq 200) $asg.Body
$bm=Call 'POST' '/Auth/login' $null @{UsernameOrEmail='bm_scope_test';Password='Branch@1234'}; $bt=Tok $bm.Raw; Add 'BM login' '200' $bm.Status ($bm.Status -eq 200 -and $bt) $bm.Body
$x=Call 'GET' '/Dashboard/merchant' $bt $null; Add 'BM /Dashboard/merchant' '200' $x.Status ($x.Status -eq 200) $x.Body
$x=Call 'GET' "/Restaurants/$rid/addresses" $bt $null; Add "BM /Restaurants/$rid/addresses" '200' $x.Status ($x.Status -eq 200) $x.Body
$x=Call 'GET' '/Restaurants/2/addresses' $bt $null; Add 'BM /Restaurants/2/addresses' '403' $x.Status ($x.Status -eq 403) $x.Body
$x=Call 'GET' "/Promotions/by-restaurant/$rid" $bt $null; Add "BM /Promotions/by-restaurant/$rid" '200' $x.Status ($x.Status -eq 200) $x.Body
$x=Call 'GET' '/Promotions/by-restaurant/2' $bt $null; Add 'BM /Promotions/by-restaurant/2' '403' $x.Status ($x.Status -eq 403) $x.Body
$R | Select Test,Expected,Actual,Pass | Format-Table -AutoSize
$F=$R|? Pass -eq 'FAIL'; if($F){'MISMATCH DETAILS'; $F|Select Test,Expected,Actual,Body|Format-Table -AutoSize -Wrap}
if($started -and $proc -and -not $proc.HasExited){ Stop-Process -Id $proc.Id -Force }
