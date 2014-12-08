using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CsvPickTests
{
    using CsvPick;


    [TestClass]
    public class FormatterTests
    {
        [TestMethod]
        public void Formatter_Simple()
        {
            var format   = AbstractProcess.CreateFormatter( "|", prependOutIndex: false );
            var inputs   = new[] { new [] { "Un", "Deux", "Troi" },
                                   new [] { "", "Dos", "" } };
            var expected = new [] { "Un|Deux|Troi", "|Dos|" };

            var actual   = format( inputs );

            NRAssert.CollectionEquals( expected, actual );
        }

        [TestMethod]
        public void Formatter_Prepend_HasFieldNum()
        {
            var format   = AbstractProcess.CreateFormatter( "+", prependOutIndex: true );
            var inputs   = new[] { new [] { "Un", "Deux", "Troi" },
                                   new [] { "", "Dos", "" } };
            var expected = new [] { "0       Un+1       Deux+2       Troi",
                                    "0       +1       Dos+2       " };

            var actual   = format( inputs );

            NRAssert.CollectionEquals( expected, actual );
        }
    }
}