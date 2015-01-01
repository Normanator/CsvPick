using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

using CsvPick;

namespace CsvPickTests
{
    [TestClass]
    public class DelimParserTests
    {
        [TestMethod]
        public void Parse_NullColumns_ReturnAllColumns()
        {
            var dp     = new DelimParser( null, columns:null );
            var fields = dp.Parse( "One,Two,Three" );
            var expected = 3;
            var actual   = fields.Count;

            Assert.AreEqual( expected, actual );
        }

        [TestMethod]
        public void Parse_AllColumns_ReturnAllColumns()
        {
            var dp     = new DelimParser( null, new [] { 0, 1, 2 } );
            var fields = dp.Parse( "One,Two,Three" );
            var expected = 3;
            var actual   = fields.Count;

            Assert.AreEqual( expected, actual );
        }

        [TestMethod]
        public void Parse_Columns_Retrieved()
        {
            var dp       = new DelimParser( null, new [] { 2, 4, 6 }, trim:true );
            var expected = new List<string>();
            expected.Add("Two");
            expected.Add("Four");
            expected.Add("Six");

            var actual   = dp.Parse( "Zero, One, Two ,\t Three\t,Four,  Five  , Six  , Seven" );

            Assert.IsTrue( expected.SequenceEqual( actual ) );
        }


        [TestMethod]
        public void Parse_Columns_MissingAreEmptyString()
        {
            var dp       = new DelimParser( null, new [] { 2, 4 } );
            var expected = new List<string>();
            expected.Add( string.Empty );
            expected.Add( string.Empty );

            var actual   = dp.Parse( "Zero,One,,Three," );

            Assert.IsTrue( expected.SequenceEqual( actual ) );
        }


        [TestMethod]
        public void Parse_Columns_Padded()
        {
            var dp       = new DelimParser( null, new [] { 2, 4 } );
            var expected = new List<string>();
            expected.Add("Two");
            expected.Add( string.Empty );

            var actual   = dp.Parse( "Zero, One, Two" );

            Assert.IsTrue( expected.SequenceEqual( actual ) );
        }


        [TestMethod]
        public void Parse_AllColumns_MissingAreEmptyString()
        {
            var dp       = new DelimParser( null, null );
            var expected = new List<string>();
            expected.Add( "Zero" );
            expected.Add( "One" );
            expected.Add( string.Empty );
            expected.Add( "Three" );
            expected.Add( string.Empty );

            var actual   = dp.Parse( "Zero,One,,Three," );

            Assert.IsTrue( expected.SequenceEqual( actual ) );
        }
    }
}
