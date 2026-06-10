# RTM tmLanguage Improvements

This document summarizes the improvements made to `RtmLanguageExtension/syntaxes/rtm.tmLanguage.json`.

## Changes Applied

### 1. ✅ Dollar Directives (Fixed)
**Before:** Included many non-existent directives from the pattern list  
**After:** Only actual directives from the RTM lexer

**Removed (not in lexer):**
- `$EXTRACT`, `$FILE`, `$LIVE`, `$LIVEUSER`, `$NEWCLUSTERS`, `$OPT`, `$RECORD`, `$SNAPSHOT`, `$TEST`, `$TESTUSER`, `$DATA,SHARED`

**Added (missing from original):**
- `$INCLUDESTART`, `$INCLUDEEND`, `$END`

**Final list:**
```
$ENTRY, $NAME, $FILES, $DATA, $EXTDATA, $SCRNDATA, $USERDATA, 
$EXT, $PROG, $ABORT, $INCLUDESTART, $INCLUDEEND, $INCLUDE, $END
```

**Scope:** `keyword.control.directive.rtm` (changed from `keyword.dollar.rtm` for better theme support)

---

### 2. ✅ DIE Keyword (Added)
**Added:** `DIE` to control.condition keywords  
**Scope:** `keyword.control.condition.rtm`  
**Grouped with:** `IF`, `THEN`, `ELSE`, `AND`, `OR`

---

### 3. ✅ Operators (Added)
**Added:** New operator pattern to highlight RTM operators

**Operators included:**
- Assignment: `_`, `=`
- Increment/Decrement: `++`, `--`
- Arithmetic: `+`, `-`, `*`, `/`, `//`, `/:`
- Compound: `+_`
- Bitwise: `&`, `!`, `%`, `<:`, `:>`
- Comparison: `#`, `<`, `>`, `<=`, `>=`

**Pattern:**
```json
{
    "name": "keyword.operator.rtm",
    "match": "(_(?=\\s)|\\+\\+|--|\\+_|&|!|%|<:|:>|#|<=|>=|//|/:|\\+|-|\\*|/|=|<|>)"
}
```

**Scope:** `keyword.operator.rtm`

---

### 4. ✅ Field Identifiers (Enhanced)
**Before:** Only supported `B@[1234ABCR]`  
**After:** Supports all @ variables and special field prefixes

**Enhanced to support:**
- `@[0-4AB]` - @ variables (e.g., `@1`, `@A`, `@B`)
- `W$`, `F$`, `H$`, `G$` field prefixes (e.g., `F1$ID`, `W$NAME`, `H2$FIELD`)

**Pattern:**
```json
{
    "name": "support.variable.rtm",
    "match": "(?<=\\b|_)(?<!\\.)(@[0-4AB]|[WFHG][0-9]?\\$[a-zA-Z][@a-zA-Z0-9.]*)(?=\\b|_)"
}
```

**Scope:** `support.variable.rtm`

---

### 5. ✅ Constant Scope Naming (Improved)
**Changed:** `constant.language.io.rtm` → `constant.language.rtm`  
**Reason:** More standard scope name for better theme compatibility

**Constants affected:** `DOWN`, `ERASE`, `CR`, `BACK`, `BEEP`, `CLEAR`, `LEFT`, `RIGHT`, `TABSTOP`, `UP`, `FLASH`, `HI`, `LO`, `KEYED`, `NOABORT`

---

### 6. ⚠️ Literals (Partially Added)
**Added:** Constants section with patterns for:
- Date literals: `"2024-01-01"D`
- Hex literals: `"FF"X` or `"FF"H`
- Key literals: `"KEY"K`
- Numeric literals: `12345`

**Status:** Section added but may need further refinement

---

## Scope Naming Strategy

The scopes were chosen to maximize compatibility with existing VS Code themes:

| RTM Element | Scope | Why |
|------------|-------|-----|
| `$DATA`, `$PROG` | `keyword.control.directive.rtm` | Treated like preprocessor directives |
| `QUITZUG`, `OVERLAY` | `keyword.reserved.rtm` | Language-reserved keywords |
| `DO`, `END`, `PROC` | `keyword.control.block.rtm` | Block structure keywords |
| `IF`, `THEN`, `DIE` | `keyword.control.condition.rtm` | Conditional keywords |
| `=`, `+`, `++` | `keyword.operator.rtm` | Operators |
| `ALLOC`, `READ` | `support.function.files.rtm` | Built-in functions |
| `@1`, `F$ID` | `support.variable.rtm` | Special variables |
| `DOWN`, `CLEAR` | `constant.language.rtm` | Language constants |
| `12345` | `constant.numeric.rtm` | Numeric literals |
| `"text"` | `string.quoted.double.rtm` | String literals |
| `<<comment>>` | `comment.block.rtm` | Comments |

---

## Testing Color Highlighting

To verify highlighting is working:

1. Open a `.rtm` file in VS Code
2. Press `Ctrl+Shift+P`
3. Run: **"Developer: Inspect Editor Tokens and Scopes"**
4. Click on various tokens to see their scopes

**Expected scopes:**
- `$DATA` → `keyword.control.directive.rtm`
- `QUITZUG` → `keyword.reserved.rtm`
- `IF` → `keyword.control.condition.rtm`
- `=` → `keyword.operator.rtm`
- `READ` → `support.function.files.rtm`
- `@1` → `support.variable.rtm`

---

## Remaining Improvements (Not Yet Applied)

### 7. Inline Comments Enhancement
**Current:** Uses `begin`/`end` pattern
**Potential improvement:** Better multi-line handling

### 8. String Escape Sequences
**Current:** No escape handling
**Potential improvement:** Highlight escape sequences in strings (if RTM supports them)

### 9. Data Area Context Scoping
**Current:** `$DATA` area has basic type matching
**Potential improvement:** Better separation of data-area-only tokens vs code tokens

### 10. Name Section Handling
**Current:** Minimal
**Potential improvement:** Special highlighting for `$NAME` sections and `$NAME ....` terminators

---

## Files Modified

- `..\RtmLanguageExtension\syntaxes\rtm.tmLanguage.json`
- Backup created: `..\RtmLanguageExtension\syntaxes\rtm.tmLanguage.json.backup`
- Working version: `..\RtmLanguageExtension\syntaxes\rtm.tmLanguage.json.working`

---

## How to Apply Updates

After making changes to `rtm.tmLanguage.json`:

1. **Reload VS Code:** Press `Ctrl+Shift+P` → "Developer: Reload Window"
2. **Or:** Restart VS Code completely
3. **Or:** Run extension in debug mode (F5)

Changes to tmLanguage grammar files require a window reload to take effect.

---

## Color Theme Compatibility

The scopes used follow TextMate naming conventions that most VS Code themes support:

- ✅ `keyword.control.*` - Usually colored
- ✅ `keyword.operator` - Usually colored
- ✅ `support.function.*` - Usually colored
- ✅ `support.variable` - Usually colored
- ✅ `constant.*` - Usually colored
- ✅ `string.quoted.*` - Usually colored
- ✅ `comment.*` - Usually colored

If a theme doesn't color certain scopes, consider:
1. Switching to a different theme
2. Adding a custom theme to the extension
3. Customizing your user settings with `editor.tokenColorCustomizations`

---

## Next Steps

To further improve the extension:

1. **Add a custom color theme** (optional) in `themes/` folder
2. **Test with multiple themes** to ensure broad compatibility
3. **Add snippets** for common RTM patterns
4. **Document the grammar** improvements in the extension README
5. **Publish updated extension** to VS Code marketplace

---

## Reference: RTM Lexer Tokens

For reference, here are the actual tokens from `RtmLexer.g4`:

**Dollar Directives:**
```
DOLLAR_ENTRY, DOLLAR_NAME, DOLLAR_FILES, DOLLAR_DATA, 
DOLLAR_EXTDATA, DOLLAR_SCRNDATA, DOLLAR_USERDATA, 
DOLLAR_EXT, DOLLAR_PROG, DOLLAR_ABORT, 
DOLLAR_INCLUDE_START, DOLLAR_INCLUDE_END, 
DOLLAR_INCLUDE, DOLLAR_END
```

**Keywords:**
```
OVERLAY, RETURN, PROC, ENDPROC, QUITZUG, DO, END, 
CASE, ENDCASE, ALWAYS, NEVER, UNTIL, WHILE, REPEAT, 
IF, THEN, ELSE, AND, OR, DIE
```

**Operators:**
```
ASSIGN: _
INCREMENT: ++
DECREMENT: --
ADD_TO: +_
PLUS: +
MINUS: -
STAR: *
SLASH: /
DOUBLE_SLASH: //
SLASH_COLON: /:
BIT_AND: &
BIT_OR: !
BIT_XOR: %
BIT_SHIFT_LEFT: <:
BIT_SHIFT_RIGHT: :>
EQUAL: =
NOT_EQUAL: #
LESS_THAN: <
MORE_THAN: >
LESS_OR_EQUAL: <=
MORE_OR_EQUAL: >=
```
