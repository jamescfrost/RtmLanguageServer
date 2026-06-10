# Unit Test Coverage Analysis

## Current Test Coverage

### ✅ Well-Tested Components (Protocol Layer)

#### 1. **WorkspaceStatus.cs** - ✅ COMPLETE
- **Test File:** `WorkspaceStatusTests.cs` (6 tests)
- Coverage:
  - ✅ Trace() factory method
  - ✅ Milestone() factory method (with/without isBusy)
  - ✅ Important() factory method
  - ✅ Empty string handling
  - ✅ All StatusKind values

#### 2. **LocationEqualityComparer.cs** - ✅ COMPLETE
- **Test File:** `LocationEqualityComparerTests.cs` (21 tests)
- Coverage:
  - ✅ Singleton pattern
  - ✅ Equality comparison (all scenarios)
  - ✅ Hash code consistency
  - ✅ Comparison/sorting logic
  - ✅ Null handling
  - ✅ Case-insensitive URI comparison
  - ✅ HashSet usage
  - ✅ List.Sort usage

#### 3. **CollectingErrorListener.cs** - ✅ COMPLETE
- **Test File:** `CollectingErrorListenerTests.cs` (10 tests)
- Coverage:
  - ✅ Diagnostic collection
  - ✅ Multiple errors
  - ✅ Source naming
  - ✅ Line number conversion
  - ✅ Range calculation
  - ✅ Lexer scenarios
  - ✅ Parser scenarios

#### 4. **RtmDiagnostic.cs** - ✅ COMPLETE
- **Test File:** `RtmDiagnosticTests.cs` (11 tests)
- Coverage:
  - ✅ Constructor
  - ✅ Record immutability
  - ✅ Equality
  - ✅ All severity levels
  - ✅ List operations
  - ✅ ToString

#### 5. **DocumentStore.cs** - ⚠️ PARTIAL
- **Test File:** `DocumentStoreTests.cs` (7 tests)
- Coverage:
  - ✅ Constructor
  - ✅ Status event subscription
  - ✅ OpenOrUpdate
  - ✅ TryGet (existing/non-existing)
  - ✅ All documents enumeration
  - ❌ LoadWorkspace (not tested)
  - ❌ EnsureFallbackWorkspaceLoaded (not tested)
  - ❌ RebuildWorkspaceAndDocumentsUnsafe (internal, indirectly tested)

#### 6. **RtmLanguageService.cs** - ⚠️ PARTIAL
- **Test File:** `RtmLanguageServiceTests.cs` (9 tests)
- Coverage:
  - ✅ Constructor
  - ✅ GetDefinition (valid/invalid)
  - ✅ FindReferences (deduplication, sorting)
  - ✅ Hover (valid/invalid)
  - ✅ ResolveSymbol helper (indirectly)
  - ✅ BuildHoverMarkdown (indirectly)
  - ❌ ToTitle (not directly tested)
  - ❌ DisplayUri (not directly tested)

#### 7. **JsonRpcServer.cs** - ⚠️ PARTIAL
- **Test File:** `JsonRpcServerTests.cs` (8 tests)
- Coverage:
  - ✅ Constructor
  - ✅ RunAsync with empty stream
  - ✅ RunAsync with cancellation
  - ✅ RunAsync with messages
  - ✅ Multiple messages
  - ❌ HandleMessageAsync (indirectly tested)
  - ❌ PublishStatus (not tested)
  - ❌ PublishDiagnostics (not tested)
  - ❌ ShouldSendStatusBarUpdate (not tested)
  - ❌ ProcessBackgroundNotificationsAsync (indirectly tested)
  - ❌ ReadMessageAsync (indirectly tested)
  - ❌ SendAsync (indirectly tested)
  - ❌ Individual LSP request handlers (not tested)

#### 8. **LspModels.cs** - ❌ NOT TESTED
- Contains DTOs - typically not unit tested directly
- Tested indirectly through integration tests

---

## ❌ Untested Components (Core Layer)

### Analysis
- **RtmScope.cs** - ❌ No tests
- **SemanticResolver.cs** - ❌ No tests
- **SymbolIndex.cs** - ❌ No tests (critical component!)

### Common
- **DiagnosticSeverity.cs** - ✅ Tested via RtmDiagnostic
- **Location.cs** - ✅ Tested via LocationEqualityComparer
- **RtmUri.cs** - ❌ No tests
- **SourceRange.cs** - ❌ No tests
- **TextPosition.cs** - ❌ No tests
- **TextRange.cs** - ❌ No tests

### Parsing
- **RtmDocument.cs** - ⚠️ Tested in integration tests only
- **RtmAnalyzer.cs** - ❌ No tests
- **RtmPreprocessor.cs** - ❌ No tests
- **RtmWorkspaceIndex.cs** - ❌ No tests
- **Extensions.cs** - ❌ No tests

### Symbols
- All symbol classes - ❌ No unit tests (tested in integration only)

---

## Test Coverage Summary

### By Count
- **Total Protocol Classes:** 5 main + 1 DTOs
- **Fully Tested:** 4 (WorkspaceStatus, LocationEqualityComparer, CollectingErrorListener, RtmDiagnostic)
- **Partially Tested:** 3 (DocumentStore, RtmLanguageService, JsonRpcServer)
- **Not Tested:** 1 (LspModels - DTOs, acceptable)

- **Total Core Classes:** ~40+
- **Tested:** 3 indirectly via Protocol tests
- **Not Tested:** ~37+

### By Layer
| Layer | Files | Fully Tested | Partially Tested | Not Tested | Coverage % |
|-------|-------|--------------|------------------|------------|------------|
| **Protocol** | 6 | 4 | 2 | 0 | ~75% |
| **Core** | ~40 | 0 | 3 | ~37 | ~10% |
| **Overall** | ~46 | 4 | 5 | ~37 | ~25% |

---

## Recommendations for Complete Coverage

### HIGH PRIORITY (Core Functionality)
1. **SymbolIndex.cs** - Critical for all symbol operations
   - AddDefinition, AddReference
   - FindDefinitions, FindReferences
   - DefinitionAt, ReferenceAt

2. **RtmUri.cs** - Used throughout for URI normalization
   - Normalize()
   - ToLocalPath()
   - SourceNameFromUri()
   - DisplayName()

3. **TextPosition.cs / TextRange.cs** - Core data structures
   - Equality, comparison
   - Contains logic
   - Range operations

4. **JsonRpcServer LSP Handlers** - Protocol implementation
   - initialize
   - textDocument/didOpen
   - textDocument/didChange
   - textDocument/definition
   - textDocument/references
   - textDocument/hover

5. **DocumentStore.LoadWorkspace** - Workspace management
   - File discovery
   - Workspace indexing
   - Status publishing during load

### MEDIUM PRIORITY
6. **SemanticResolver.cs** - Symbol resolution
7. **RtmDocument.Parse** - Direct unit tests
8. **RtmWorkspaceIndex** - Workspace state management
9. **SourceRange.cs** - Extension methods

### LOW PRIORITY (Complex/Integration-level)
10. **RtmAnalyzer.cs** - Tree walker (integration test level)
11. **RtmPreprocessor.cs** - Text transformation (integration test level)
12. **Symbol classes** - Mostly data structures (integration test level)

---

## Missing Test Count Estimate

To achieve **90%+ coverage**, we need approximately:

- **JsonRpcServer:** +15 tests (LSP handlers, message processing)
- **SymbolIndex:** +12 tests (all public methods)
- **RtmUri:** +8 tests (all public methods)
- **TextPosition/Range:** +10 tests (data structure operations)
- **DocumentStore:** +5 tests (LoadWorkspace, workspace handling)
- **SemanticResolver:** +8 tests (resolution logic)
- **RtmDocument:** +6 tests (direct parsing tests)
- **SourceRange:** +5 tests (extension methods)

**Total: ~69 additional tests needed for comprehensive coverage**

---

## Current Status
✅ **Protocol Layer Refactored Components:** Well tested (64+ tests)  
⚠️ **Core Layer:** Needs significant test coverage  
⚠️ **Integration Points:** Need more thorough testing

**Overall Assessment:** Good coverage for recently refactored components, but Core layer needs comprehensive unit testing.
