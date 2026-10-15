param([double]$CenterLongitude=21.1854, [double]$CenterLatitude=44.6199926, [string]$InputXml, [string]$Output = (Join-Path $PSScriptRoot '../../Assets/CityGame/Resources/Maps/PozarevacStreets.json'))
$ErrorActionPreference='Stop'
$bbox=([string]::Format([Globalization.CultureInfo]::InvariantCulture,'{0:F7},{1:F7},{2:F7},{3:F7}', $CenterLongitude-0.0036, $CenterLatitude-0.0025, $CenterLongitude+0.0036, $CenterLatitude+0.0025))
$temporary=$null
try {
    if(-not $InputXml) {
        $temporary=Join-Path ([IO.Path]::GetTempPath()) ('vkt-osm-'+[guid]::NewGuid().ToString('N')+'.xml')
        Invoke-WebRequest -Uri "https://api.openstreetmap.org/api/0.6/map?bbox=$bbox" -OutFile $temporary -TimeoutSec 60 -Headers @{'User-Agent'='VKTOfflineMapImporter/1.0'}
        $InputXml=$temporary
    }
    $settings=[Xml.XmlReaderSettings]::new();$settings.DtdProcessing=[Xml.DtdProcessing]::Prohibit;$settings.XmlResolver=$null
    $reader=[Xml.XmlReader]::Create((Resolve-Path -LiteralPath $InputXml).Path,$settings)
    try {$xml=[Xml.XmlDocument]::new();$xml.XmlResolver=$null;$xml.Load($reader)}finally{$reader.Dispose()}
    $nodes=@{}
    $culture=[Globalization.CultureInfo]::InvariantCulture
    foreach($node in $xml.osm.node){$nodes[[string]$node.id]=@([double]::Parse($node.lon,$culture),[double]::Parse($node.lat,$culture))}
    $roads=[Collections.Generic.List[object]]::new();$segments=0
    foreach($way in $xml.osm.way){
        $tags=@{};foreach($tag in $way.tag){$tags[[string]$tag.k]=[string]$tag.v}
        if(-not $tags.ContainsKey('highway') -or $tags.highway -notin @('residential','tertiary','secondary','primary','unclassified','living_street','pedestrian','service','footway')){continue}
        $points=[Collections.Generic.List[object]]::new()
        foreach($nd in $way.nd){
            $coord=$nodes[[string]$nd.ref];if(-not $coord){continue}
            $x=($coord[0]-$CenterLongitude)*111320*[Math]::Cos( $CenterLatitude*[Math]::PI/180);$z=($coord[1]-$CenterLatitude)*111320
            $points.Add([ordered]@{x=[Math]::Round($x,3);z=[Math]::Round($z,3)})
        }
        if($points.Count -lt 2){continue}
        $name=$tags['name:sr-Latn'];if(-not $name){$name=$tags['name']};if(-not $name){$name=''}
        # Split at excluded edges: never bridge a gap or connect unrelated nodes.
        for($i=1;$i -lt $points.Count;$i++){
            $a=$points[$i-1];$b=$points[$i]
            if([Math]::Abs($a.x) -gt 285 -or [Math]::Abs($a.z) -gt 285 -or [Math]::Abs($b.x) -gt 285 -or [Math]::Abs($b.z) -gt 285){continue}
            if($segments -ge 200){break}
            $roads.Add([ordered]@{id=[string]$way.id;name=$name;kind=$tags.highway;points=@($a,$b)})
            $segments++
        }
        if($segments -ge 200){break}
    }
    if($segments -eq 0){throw 'No bounded public OSM highway segments found.'}
    $buildings=[Collections.Generic.List[object]]::new()
    foreach($way in $xml.osm.way){
        if($buildings.Count -ge 40){break}
        $tags=@{};foreach($tag in $way.tag){$tags[[string]$tag.k]=[string]$tag.v}
        if(-not $tags.ContainsKey('building') -or $tags.building -eq 'no'){continue}
        $polygon=[Collections.Generic.List[object]]::new();$valid=$true
        foreach($nd in $way.nd){$coord=$nodes[[string]$nd.ref];if(-not $coord){$valid=$false;break};$x=($coord[0]-$CenterLongitude)*111320*[Math]::Cos($CenterLatitude*[Math]::PI/180);$z=($coord[1]-$CenterLatitude)*111320;if([Math]::Abs($x)-gt285-or[Math]::Abs($z)-gt285){$valid=$false;break};$polygon.Add([ordered]@{x=[Math]::Round($x,3);z=[Math]::Round($z,3)})}
        if(-not $valid-or$polygon.Count-lt4-or$polygon.Count-gt80){continue}
        if($polygon[0].x-ne$polygon[$polygon.Count-1].x-or$polygon[0].z-ne$polygon[$polygon.Count-1].z){continue}
        $height=9.0;$heightSource='authored-default';$parsed=0.0
        if($tags.height-and[double]::TryParse($tags.height,[Globalization.NumberStyles]::Float,$culture,[ref]$parsed)){$height=$parsed;$heightSource='osm-height'}
        elseif($tags['building:levels']-and[double]::TryParse($tags['building:levels'],[Globalization.NumberStyles]::Float,$culture,[ref]$parsed)){$height=$parsed*3;$heightSource='osm-levels-times-3m'}
        $buildings.Add([ordered]@{id=[string]$way.id;height=[Math]::Clamp($height,3,40);heightSource=$heightSource;points=$polygon.ToArray()})
    }
    $data=[ordered]@{schema=1;source='https://api.openstreetmap.org/api/0.6/map?bbox='+$bbox;downloadedUtc=[DateTime]::UtcNow.ToString('o');bbox=$bbox;anchorLongitude=$CenterLongitude;anchorLatitude=$CenterLatitude;radiusMeters=285;attribution='© OpenStreetMap contributors';license='ODbL-1.0';licenseUrl='https://www.openstreetmap.org/copyright';roads=$roads.ToArray();buildings=$buildings.ToArray()}
    $target=[IO.Path]::GetFullPath($Output);[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($target))|Out-Null
    [IO.File]::WriteAllText($target,($data|ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false))
    Write-Output "Imported $segments road segments into $target; no contributor identity fields retained."
} finally {if($temporary -and [IO.File]::Exists($temporary)){[IO.File]::Delete($temporary)}}
