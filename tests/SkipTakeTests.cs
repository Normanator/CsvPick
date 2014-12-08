using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CsvPickTests
{
    using CsvPick;

    [TestClass]
    public class SkipTakeTests
    {
        [TestMethod]
        public void SkipTake_Comments_Ignored()
        {
            var skipTake = AbstractProcess.CreateSkipTake( "#", 0, 0, 100.0, 0 );
            var inputs   = InputterTests.AsNumbLines(
                new[] { "# Comment1", "# Comment2", "Line3", "Line4", "# Comment5", "Line6" } );

            var expected = new[] { new NumberedLine( 3, "Line3" ),
                                   new NumberedLine( 4, "Line4" ),
                                   new NumberedLine( 6, "Line6" ) };

            var actual   = skipTake( inputs );

            NRAssert.AreEqual( expected, actual );
        }

        [TestMethod]
        public void SkipTake_CommentsPreSkip_Ignored()
        {
            var skipTake = AbstractProcess.CreateSkipTake( "#", 2, 3, 100.0, 0 );
            var inputs   = InputterTests.AsNumbLines(
                new[] { "# Comment1", "# Comment2", "Header3", "Header4",
                        "Line5", "Line6", "Line7", "Line8" } );

            var expected = new[] { new NumberedLine( 5, "Line5" ),
                                   new NumberedLine( 6, "Line6" ),
                                   new NumberedLine( 7, "Line7" ) };

            var actual   = skipTake( inputs );

            NRAssert.AreEqual( expected, actual );
        }
    }
}