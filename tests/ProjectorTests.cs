using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CsvPickTests
{
    using CsvPick;

    [TestClass]
    public class ProjectorTests
    {
            [TestMethod]
        public void Projector_NullColumns_AllColumns()
        {
            var project  = AbstractProcess.CreateProjector( null );
            var inputs   = InputterTests.AsNumbRecs(
                new[] { Tuple.Create( "R1", new [] { "1.0", "1.1", "1.2", "1.3" } ),
                        Tuple.Create( "R2", new [] { "2.0", "2.1", "2.2", "2.3" } ),
                        Tuple.Create( "R3", new [] { "3.0", "3.1", "3.2", "3.3" } ) } );
            var expected = InputterTests.AddOutFields( 
                                inputs,
                                new[] { new [] { "1.0", "1.1", "1.2", "1.3" },
                                        new [] { "2.0", "2.1", "2.2", "2.3" },
                                        new [] { "3.0", "3.1", "3.2", "3.3" } } );

            var actual   = project( inputs );

            NRAssert.AreEqual( expected, actual );
        }

        [TestMethod]
        public void Projector_Cols1_3_TwoCols()
        {
            var project  = AbstractProcess.CreateProjector( new [] { 1, 3 } );
            var inputs   = InputterTests.AsNumbRecs(
                new[] { Tuple.Create( "R1", new [] { "1.0", "1.1", "1.2", "1.3" } ),
                        Tuple.Create( "R2", new [] { "2.0", "2.1", "2.2", "2.3" } ),
                        Tuple.Create( "R3", new [] { "3.0", "3.1", "3.2", "3.3" } ) } );
            var expected = InputterTests.AddOutFields( 
                                inputs,
                                new[] { new [] { "1.1", "1.3" },
                                        new [] { "2.1", "2.3" },
                                        new [] { "3.1", "3.3" } } );

            var actual   = project( inputs );

            NRAssert.AreEqual( expected, actual );
        }

        [TestMethod]
        public void Projector_ColsOutOfOrder_Preserved()
        {
            var project  = AbstractProcess.CreateProjector( new [] { 3, 1, 1 } );
            var inputs   = InputterTests.AsNumbRecs(
                new[] { Tuple.Create( "R1", new [] { "1.0", "1.1", "1.2", "1.3" } ),
                        Tuple.Create( "R2", new [] { "2.0", "2.1", "2.2", "2.3" } ),
                        Tuple.Create( "R3", new [] { "3.0", "3.1", "3.2", "3.3" } ) } );
            var expected = InputterTests.AddOutFields( 
                                inputs,
                                new[] { new [] { "1.3", "1.1", "1.1" },
                                        new [] { "2.3", "2.1", "2.1" },
                                        new [] { "3.3", "3.1", "3.1" } } );

            var actual   = project( inputs );

            NRAssert.AreEqual( expected, actual );
        }

        [TestMethod]
        public void Projector_Minus1_LineNums()
        {
            var project  = AbstractProcess.CreateProjector( new [] { 0, 1, -1 } );
            var inputs   = InputterTests.AsNumbRecs(
                new[] { Tuple.Create( "R1", new [] { "1.0", "1.1" } ),
                        Tuple.Create( "R2", new [] { "2.0", "2.1" } ),
                        Tuple.Create( "R3", new [] { "3.0", "3.1" } ) } );
            var expected = InputterTests.AddOutFields( 
                                inputs,
                                new[] { new [] { "1.0", "1.1", "1" },
                                        new [] { "2.0", "2.1", "2" },
                                        new [] { "3.0", "3.1", "3" } } );

            var actual   = project( inputs );

            NRAssert.AreEqual( expected, actual );
        }
    }
}