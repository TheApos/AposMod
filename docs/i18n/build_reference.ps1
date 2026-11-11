# Builds docs/Reference.html from the parts in this folder:
#   head.html          - <title>, fonts, <style>, the SVG icon sprite
#   body.<lang>.html   - header, nav, sections and footer in English (default), Russian and Ukrainian
#   datauris.txt       - images embedded in the bodies (DATAURI_<n> placeholders, one data URI per line)
#   script.js          - language switch, search, icons, tiles and hover hints
# Edit the parts, then run: & docs/i18n/build_reference.ps1
$ErrorActionPreference = 'Stop'
$dir = $PSScriptRoot
$utf8 = New-Object Text.UTF8Encoding($false)
$uris = [IO.File]::ReadAllLines((Join-Path $dir 'datauris.txt'))
# {{VERSION}} in the bodies = the mod version from ModInfo.xml, so the page never shows an old one
$version = ([xml][IO.File]::ReadAllText((Join-Path $dir '..\..\ModInfo.xml'))).xml.Version.value
if (-not $version) { throw 'No Version in ModInfo.xml' }
function Body($lang) {
  $b = [IO.File]::ReadAllText((Join-Path $dir "body.$lang.html"))
  $b = [regex]::Replace($b, 'DATAURI_(\d+)', { param($m) $uris[[int]$m.Groups[1].Value] })
  if ($b -match 'DATAURI_') { throw "unresolved image placeholder in body.$lang.html" }
  $b = $b.Replace('{{VERSION}}', $version)
  "<template id=`"lang-$lang`">`n$b</template>`n"
}
$css = @'
<style>
.langs { display: inline-flex; gap: 4px; margin-left: 12px; vertical-align: middle; }
.langs button { font: 600 11px var(--f-mono); letter-spacing: 1px; padding: 2px 7px; border: 1px solid var(--line); border-radius: 3px; background: var(--panel); color: var(--ink); cursor: pointer; }
.langs button[aria-pressed="true"] { border-color: var(--amber); color: var(--amber); }
.langs button:focus-visible { outline: 2px solid var(--amber); outline-offset: 1px; }
</style>
'@
$page = [IO.File]::ReadAllText((Join-Path $dir 'head.html')) + $css + "<div id=`"app`"></div>`n" +
  (Body 'en') + (Body 'ru') + (Body 'uk') +
  "<script>`n" + [IO.File]::ReadAllText((Join-Path $dir 'script.js')) + "</script>`n"
$out = Join-Path $dir '..\Reference.html'
[IO.File]::WriteAllText($out, $page, $utf8)
"Reference.html: $($page.Length) chars, version $version"
