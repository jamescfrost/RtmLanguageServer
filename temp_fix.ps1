$content = Get-Content "..\RtmLanguageExtension\syntaxes\rtm.tmLanguage.json" -Raw
$old = "},
{
`"name`": `"constant.language.io.rtm`","
$new = "},
{
`"name`": `"keyword.operator.rtm`",
`"match`": `"(_(?=\\s)|\\+\\+|--|\\+_|&|!|%|<:|:>|#|<=|>=|//|/:|\\+|-|\\*|/|=|<|>)`"
},
{
`"name`": `"constant.language.io.rtm`","
$content = $content.Replace($old, $new)
$content | Set-Content "..\RtmLanguageExtension\syntaxes\rtm.tmLanguage.json" -NoNewline
