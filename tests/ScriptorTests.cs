using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CsvPickTests
{
    using CsvPick;

    [TestClass]
    public class ScriptorTests
    {
        [TestMethod]
        public void Scriptor_Script1Proj_Invoked()
        {
            //var project  = AbstractProcess.CreateProjector( new [] { 3, -1, 2, 2  }, "_rev_script.cs" );
            var script   = AbstractProcess.CreateScriptor( "_rev_script.cs", null );
            var inputs   = InputterTests.AsNumbRecs(
                new[] { Tuple.Create( "R1", new [] { "1.0", "abc", "1.2xx"  } ),
                        Tuple.Create( "R2", new [] { "2.0", "def", "2.2xxx" } ),
                        Tuple.Create( "R3", new [] { "3.0", "ghi", "3.2",   } ) } );
            var expected = new[] { new NumberedRecord( 1, "R1", new [] { "0.1", "cba", "xx2.1",  "4" } ),
                                   new NumberedRecord( 2, "R2", new [] { "0.2", "fed", "xxx2.2", "5" } ),
                                   new NumberedRecord( 3, "R3", new [] { "0.3", "ihg", "2.3",    "3" } ) };

            var actual   = script( inputs );

            NRAssert.Equals( expected, actual );
        }

        [TestMethod]
        public void Scriptor_ScriptMultiProj_Invoked()
        {
            var script   = AbstractProcess.CreateScriptor( "_rev_script.cs", null );
            var inputs   = InputterTests.AsNumbRecs(
                new[] { Tuple.Create( "R1", new [] { "good", "a/b/c", "alpha" } ),
                        Tuple.Create( "R2", new [] { "ick",  "d/e/f", "beta" } ),
                        Tuple.Create( "R3", new [] { "good", "", "gamma" } ) } );
            var expected = new[] { new NumberedRecord( 1, "R1", new [] { "a", "alpha" } ),
                                   new NumberedRecord( 1, "R1", new [] { "b", "alpha" } ),
                                   new NumberedRecord( 1, "R1", new [] { "c", "alpha" } ),
                                   new NumberedRecord( 1, "R1", new [] { "",  "gamma" } )  };

            var actual   = script( inputs );

            NRAssert.Equals( expected, actual );
        }


        [TestMethod]
        public void Scriptor_Exception_HasLineInfo()
        {
            var script   = AbstractProcess.CreateScriptor( "_throw_script.cs", null );
            var inputs   = InputterTests.AsNumbRecs(
                new[] { Tuple.Create( "R1", new [] { "good" } ),
                        Tuple.Create( "R2", new [] { "good" } ),
                        Tuple.Create( "R3", new [] { "good" } ),
                        Tuple.Create( "R4", new [] { "ick"  } ),
                        Tuple.Create( "R5", new [] { "too-far" } ) },
                        echo: true );

            bool caught = false;
            try
            {
                var actual   = script( inputs ).ToArray();
            }
            catch( Exception ex )
            {
                Assert.IsNotNull( ex.InnerException as ApplicationException );
                Assert.IsTrue( ex.InnerException.Message.Contains( "ick encountered") );
                Assert.IsTrue( ex.Message.Contains( "col 123" ), "column info correct" );
                Assert.IsTrue( ex.Message.Contains( "line 4 " ), "line info correct" );
                Assert.IsTrue( ex.Message.Contains( "R4" ), "line echoed" );
                caught = true;
            }
            Assert.IsTrue( caught, "Expected to catch exception" );
        }

        [TestMethod]
        public void Scriptor_ErrHandler_Continues()
        {
            var                sb         = new System.Text.StringBuilder();
            Action<Exception>  errHandler = (Exception ex) =>
                               { sb.AppendLine( ex.Message ); };

            var script   = AbstractProcess.CreateScriptor( "_throw_script.cs", errHandler );
            var inputs   = InputterTests.AsNumbRecs(
                new[] { Tuple.Create( "R1", new [] { "good" } ),
                        Tuple.Create( "R2", new [] { "good" } ),
                        Tuple.Create( "R3", new [] { "good" } ),
                        Tuple.Create( "R4", new [] { "ick"  } ),
                        Tuple.Create( "R5", new [] { "too-far" } ) },
                        echo: true );
            var expected = new[] { new [] { "good" },
                                   new [] { "good" },
                                   new [] { "good" },
                                   new [] { "too-far" } };

            var actual   = script( inputs ).ToArray();

            NRAssert.NestedEquals( expected, actual );
            //Assert.IsTrue( expected.SequenceEqual( actual ), "Script output unexpected" );

            Assert.AreEqual( "Error in col 123, line 4 = (\"R4...\")\r\n",
                             sb.ToString(), 
                             "Err msg incorrect" );
        }
    }
}