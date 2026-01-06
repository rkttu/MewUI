param(
  [Parameter(Mandatory = $true)]
  [string]$Dir,

  [int]$Runs = 50
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$re = [regex]'^\[(?<ts>[^\]]+)\]\s+Metrics\s+\((?<backend>[^)]+)\):\s+Loaded\s+(?<loaded>(?:\d+|n/a))\s+ms,\s+FirstFrame\s+(?<first>\d+)\s+ms,\s+WS\s+(?<ws>[\d.]+)\s+MB,\s+Private\s+(?<ps>[\d.]+)\s+MB\s*$'

function Avg([double[]]$values) {
  $values = @($values)
  if ($values.Count -eq 0) { return [double]::NaN }
  return [double](($values | Measure-Object -Average).Average)
}

function Pct([double[]]$values, [double]$p) {
  $values = @($values | Sort-Object)
  if ($values.Count -eq 0) { return [double]::NaN }
  $rank = ($p / 100.0) * ($values.Count - 1)
  $lo = [int][math]::Floor($rank)
  $hi = [int][math]::Ceiling($rank)
  if ($lo -eq $hi) { return [double]$values[$lo] }
  $w = $rank - $lo
  return ([double]$values[$lo] * (1 - $w)) + ([double]$values[$hi] * $w)
}

function Parse([string]$path) {
  if (-not (Test-Path -LiteralPath $path)) { return @() }
  $rows = New-Object System.Collections.Generic.List[object]
  foreach ($line in (Get-Content -LiteralPath $path)) {
    $m = $re.Match($line)
    if (-not $m.Success) { continue }
    $loaded = $m.Groups["loaded"].Value
    $rows.Add([pscustomobject]@{
      Backend  = $m.Groups["backend"].Value
      LoadedMs = if ($loaded -eq "n/a") { [double]::NaN } else { [double]$loaded }
      FirstMs  = [double]$m.Groups["first"].Value
      WS_MiB   = [double]$m.Groups["ws"].Value
      PS_MiB   = [double]$m.Groups["ps"].Value
    }) | Out-Null
  }
  return $rows
}

$Dir = (Resolve-Path -LiteralPath $Dir).Path
$d2d = Join-Path $Dir "metrics_d2d.log"
$gdi = Join-Path $Dir "metrics_gdi.log"

$rows = @(Parse $d2d) + @(Parse $gdi)
$backends = @("Direct2D", "GDI") | Where-Object { $rows.Backend -contains $_ }

$out = New-Object System.Text.StringBuilder
[void]$out.AppendLine("# MewUI Trimmed Benchmark")
[void]$out.AppendLine("")
[void]$out.AppendLine(("Generated: {0:yyyy-MM-dd HH:mm:ss}" -f (Get-Date)))
[void]$out.AppendLine("")
[void]$out.AppendLine(("Runs per backend: **{0}**" -f $Runs))
[void]$out.AppendLine("")
[void]$out.AppendLine("| Backend | Loaded avg | Loaded p95 | FirstFrame avg | FirstFrame p95 | WS avg | WS p95 | PS avg | PS p95 |")
[void]$out.AppendLine("|---|---:|---:|---:|---:|---:|---:|---:|---:|")

if ($rows.Count -eq 0) {
  [void]$out.AppendLine("| (no data) | n/a | n/a | n/a | n/a | n/a | n/a | n/a | n/a |")
}

foreach ($backend in $backends) {
  $s = $rows | Where-Object Backend -eq $backend
  $loaded = @($s | Where-Object { -not [double]::IsNaN($_.LoadedMs) } | ForEach-Object LoadedMs)
  $first = @($s | ForEach-Object FirstMs)
  $ws = @($s | ForEach-Object WS_MiB)
  $ps = @($s | ForEach-Object PS_MiB)

  $la = Avg $loaded
  $lp = Pct $loaded 95
  $fa = Avg $first
  $fp = Pct $first 95
  $wa = Avg $ws
  $wp = Pct $ws 95
  $pa = Avg $ps
  $pp = Pct $ps 95

  $laStr = if ([double]::IsNaN($la)) { "n/a" } else { "{0:0}" -f $la }
  $lpStr = if ([double]::IsNaN($lp)) { "n/a" } else { "{0:0}" -f $lp }

  [void]$out.AppendLine((
    "| {0} | {1} | {2} | {3:0} | {4:0} | {5:0.0} | {6:0.0} | {7:0.0} | {8:0.0} |" -f
      $backend, $laStr, $lpStr, $fa, $fp, $wa, $wp, $pa, $pp
  ))
}

[void]$out.AppendLine("")
[void]$out.AppendLine("Raw logs:")
[void]$out.AppendLine("- metrics_d2d.log")
[void]$out.AppendLine("- metrics_gdi.log")

$report = Join-Path $Dir "bench_report.md"
$out.ToString() | Set-Content -LiteralPath $report -Encoding UTF8
Write-Host ("Report: " + $report)
