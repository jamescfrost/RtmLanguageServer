using RtmLanguageServer.Core.Parsing;
using RtmLanguageServer.Core.Symbols;
using Xunit;

namespace RtmLanguageServer.Tests;

public sealed class RtmSemanticTests
{
    private const string Sample = """
                                  RTM

                                  $NAME JAMES.DATA
                                  RANDOM.FIELD N3

                                  $ENTRY JAMES.TEST

                                  $DATA
                                  ID N5

                                  $PROG()
                                  ID = 12345
                                  QUITZUG

                                  TEST PROC(ID)
                                  RETURN()
                                  ENDPROC

                                  $ABORT
                                  RETURN()
                                  """;

    [Fact]
    public void Parses_overlay_and_program()
    {
        var document = RtmDocument.Parse("file:///sample.rtm", Sample);

        var overlay = Assert.Single(document.Index.AllDefinitions.OfType<OverlaySymbol>());
        Assert.Equal("JAMES.TEST", overlay.Name);
        Assert.NotNull(overlay.Program);
    }

    [Fact]
    public void Parameters_are_field_references_not_global_name_definitions()
    {
        var document = RtmDocument.Parse("file:///sample.rtm", Sample);

        var procedure = document.Index.AllDefinitions
            .OfType<ProcedureSymbol>()
            .Single(p => p.Name == "TEST");

        var parameter = Assert.Single(procedure.Parameters);

        Assert.Equal("ID", parameter.Name);
    }

    [Fact]
    public void Data_fields_are_indexed_as_overlay_children()
    {
        var document = RtmDocument.Parse("file:///sample.rtm", Sample);
        var overlay = document.Index.AllDefinitions.OfType<OverlaySymbol>().Single();

        var field = Assert.Single(overlay.Fields.Where(f => f.Name == "ID"));
        Assert.Equal("N5", field.EditMask);
    }

    [Fact]
    public void Includes_are_symbols()
    {
        var document = RtmDocument.Parse("file:///include.rtm", """
                                                                RTM
                                                                $ENTRY MAIN
                                                                $INCLUDE COMMON(HELLO)
                                                                $PROG()
                                                                RETURN()
                                                                """);

        var include = Assert.Single(document.Index.AllDefinitions.OfType<IncludeReference>());
        Assert.Equal("HELLO", include.Name);
        Assert.Equal("COMMON", include.SourceName);
    }
}
