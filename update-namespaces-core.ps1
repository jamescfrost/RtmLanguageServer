# Update namespaces in RtmLanguageServer.Core project

$replacements = @{
    'namespace RtmLanguageServer.Analysis.Core;' = 'namespace RtmLanguageServer.Core.Common;'
    'namespace RtmLanguageServer.Analysis.Symbols;' = 'namespace RtmLanguageServer.Core.Symbols;'
    'namespace RtmLanguageServer.Analysis;' = 'namespace RtmLanguageServer.Core.Parsing;'
    'namespace RtmLanguageServer.Analysis' = 'namespace RtmLanguageServer.Core.Analysis'
    'using RtmLanguageServer.Analysis.Core;' = 'using RtmLanguageServer.Core.Common;'
    'using RtmLanguageServer.Analysis.Symbols;' = 'using RtmLanguageServer.Core.Symbols;'
    'using RtmLanguageServer.Analysis;' = 'using RtmLanguageServer.Core.Parsing;'
    'using RtmLanguageServer.Generated;' = 'using RtmLanguageServer.Core.Generated;'
}

$files = Get-ChildItem -Path "src\RtmLanguageServer.Core" -Recurse -Filter "*.cs"

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $originalContent = $content

    foreach ($key in $replacements.Keys) {
        $content = $content -replace [regex]::Escape($key), $replacements[$key]
    }

    if ($content -ne $originalContent) {
        Set-Content -Path $file.FullName -Value $content -NoNewline
        Write-Output "Updated: $($file.Name)"
    }
}

Write-Output "`nNamespace updates complete!"
