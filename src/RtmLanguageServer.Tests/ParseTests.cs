using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using RtmLanguageServer.Analysis;
using RtmLanguageServer.Analysis.Symbols;
using RtmLanguageServer.Generated;
using Xunit;

namespace RtmLanguageServer.Tests
{
    public class ParseTests
    {
        private readonly string _source2 = """
                                           RTM
                                           $NAME JAMES.DATA
                                           $NAME JAMES.PROCS
                                           $NAME ....
                                           """.TrimStart();

        private readonly string _source = """
                                          RTM

                                          $NAME JAMES.DATA
                                            RANDOM.FIELD N3

                                          $NAME JAMES.PROCS
                                            RANDOM.PROC PROC(NAME)
                                              RETURN()
                                            ENDPROC

                                          $NAME JAMES.CODE
                                            ID_ ID+1

                                          $NAME ....

                                          $ENTRY JAMES.TEST

                                          $FILES
                                            JAMES.DATAFILE

                                          $EXT
                                            JAMES.TEST2

                                          $DATA
                                            ID N5
                                            NAME A20
                                            AGE N3
                                          $INCLUDE *(JAMES.DATA)
                                          $INCLUDE OTHERSOURCE(OTHER.DATA)

                                          $PROG(ID)
                                            ID = 12345
                                            NAME = "James"
                                            AGE = 30
                                            TEST(1)
                                          QUITZUG

                                          $INCLUDE *(JAMES.PROCS)

                                          TEST PROC(ID)
                                          $INCLUDE *(JAMES.CODE)
                                            JAMES.TEST2(NAME)
                                            RANDOM.FIELD = 456
                                            RETURN()
                                          ENDPROC

                                          $ABORT
                                            
                                          """.Trim();

        [Fact]
        public void Stuff()
        {
            var input = CharStreams.fromString(_source);
            var lexer = new RtmLexer(input);

            foreach (var token in lexer.GetAllTokens())
            {
                if (token.Channel == TokenConstants.DefaultChannel)
                {
                    Console.Error.WriteLine(
                        $"{lexer.Vocabulary.GetSymbolicName(token.Type)} : {token.Text}");
                }
            }

            lexer.Reset();

            var parser = new RtmParser(new CommonTokenStream(lexer));
            parser.RemoveErrorListeners();
            parser.AddErrorListener(new DiagnosticErrorListener());

            var tree = parser.start();

            DumpTree(tree, parser);
        }

        private static void DumpTree(IParseTree tree, Parser parser, int indent = 0)
        {
            var text = tree is ParserRuleContext ctx
                ? parser.RuleNames[ctx.RuleIndex]
                : tree.GetText();

            Console.WriteLine($"{new string(' ', indent * 2)}{text}");

            for (var i = 0; i < tree.ChildCount; i++)
                DumpTree(tree.GetChild(i), parser, indent + 1);
        }

        [Fact]
        public void Parse_document_captures_include_references()
        {
            var document = RtmDocument.Parse("file:///sample.rtm", _source);

            var includes = document.Index.AllReferences
                .OfType<IncludeReference>()
                .ToList();

            Assert.Collection(
                includes,
                include => Assert.Equal("JAMES.DATA", include.Name),
                include => Assert.Equal("OTHER.DATA", include.Name),
                include => Assert.Equal("JAMES.PROCS", include.Name),
                include => Assert.Equal("JAMES.CODE", include.Name));
        }
    }
}