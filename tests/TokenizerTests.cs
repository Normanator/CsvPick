using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CsvPickTests
{
    using CsvPick;

    [TestClass]
    public class TokenizerTests
    {
        [TestMethod]
        public void Tokenizer_Plain_NoTrim()
        {
            var tokenizer = AbstractProcess.CreateTokenizer( 
                '|', FieldParseType.Plain, new[] { 1, 3, 5 }, false, null );

            var inLines = new [] {
                new NumberedLine( 1, "zero|one|two|three|four|five" ),
                new NumberedLine( 2, "|\"1\"||\"3\"|\"4\"|\"5\"" ),
                new NumberedLine( 3, "zero||two||four|\"5|5b|5c\"" ),
            };

            var expected = new [] {
                new NumberedRecord( inLines[0], new [] { "one", "three", "five" } ),
                new NumberedRecord( inLines[1], new [] { "\"1\"", "\"3\"", "\"5\"" } ),
                new NumberedRecord( inLines[2], new [] { "", "", "\"5" } )
            };

            var actual = tokenizer( inLines );

            NRAssert.AreEqual( expected, actual );
        }

        [TestMethod]
        public void Tokenizer_Plain_Trim_Unquoted()
        {
            var tokenizer = AbstractProcess.CreateTokenizer(
                '|', FieldParseType.Plain, new[] { 1, 3, 5 }, true, null );

            var inLines = new [] {
                new NumberedLine( 1, "zero|one|two|three|four|five" ),
                new NumberedLine( 2, "|\"1\"||\"3\"|\"4\"|\"5\"" ),
                new NumberedLine( 3, "zero||two||four|\"5|5b|5c\"" ),
            };

            var expected = new [] {
                new NumberedRecord( inLines[0], new [] { "one", "three", "five" } ),
                new NumberedRecord( inLines[1], new [] { "1", "3", "5" } ),
                new NumberedRecord( inLines[2], new [] { "", "", "\"5" } )
            };

            var actual = tokenizer( inLines );

            NRAssert.AreEqual( expected, actual );
        }

        [TestMethod]
        public void Tokenizer_Quoted_Trim_InnerDelimsPreserved()
        {
            var tokenizer = AbstractProcess.CreateTokenizer( 
                '|', FieldParseType.Quoted, new[] { 1, 3, 5 }, true, null );

            var inLines = new [] {
                new NumberedLine( 1, "zero|one|two|three|four|five" ),
                new NumberedLine( 2, "|\"1\"||\"3\"|\"4\"|\"5\"" ),
                new NumberedLine( 3, "zero||two||four|\"5|5b|5c\"" ),
            };

            var expected = new [] {
                new NumberedRecord( inLines[0], new [] { "one", "three", "five" } ),
                new NumberedRecord( inLines[1], new [] { "1", "3", "5" } ),
                new NumberedRecord( inLines[2], new [] { "", "", "5|5b|5c" } )
            };

            var actual = tokenizer( inLines );

            NRAssert.AreEqual( expected, actual );
        }

        [TestMethod]
        public void Tokenizer_Json_Trim_InnerDelimsPreserved()
        {
            var tokenizer = AbstractProcess.CreateTokenizer( 
                ',', FieldParseType.Json, new[] { 0, 2 }, true, null );

            var inLines = new [] {
                new NumberedLine( 1, "zero,one,two,three" ),
                new NumberedLine( 2, "{\"a\":0, \"b\":\"hi there\"},1," ),
                new NumberedLine( 3, ",1,{foo:\"1,2,3\", baz:[{k:\"2\", l:\"2\"}, {k:\"2b\" l:\"2b\"}]},333" ),
            };

            var expected = new [] {
                new NumberedRecord( inLines[0], new [] { "zero", "two" } ),
                new NumberedRecord( inLines[1], new [] { "{\"a\":0, \"b\":\"hi there\"}", "" } ),
                new NumberedRecord( inLines[2], new [] { "", "{foo:\"1,2,3\", baz:[{k:\"2\", l:\"2\"}, {k:\"2b\" l:\"2b\"}]}" } )
            };

            var actual = tokenizer( inLines );

            NRAssert.AreEqual( expected, actual );
        }

        [TestMethod]
        public void Tokenizer_Missing_Padded()
        {
            var tokenizer = AbstractProcess.CreateTokenizer( 
                '|', FieldParseType.Quoted, new[] { 2, 4, 5 }, true, null );
            var inLines = new[] {
                new NumberedLine( 1, "zero|one|two|three" ) };
            var expected = new[] {
                new NumberedRecord( inLines[0], new [] { "two", "", "" } ) };

            var actual = tokenizer( inLines );

            NRAssert.AreEqual( expected, actual );
        }

        [TestMethod]
        public void Tokenizer_Exception_HasLineAndCol()
        {
            var tokenizer = AbstractProcess.CreateTokenizer( 
                '|', FieldParseType.Quoted, new[] { 0, 2 }, true, null );
            var inLines = new [] {
                new NumberedLine( 1, "zero|one|two" ),
                new NumberedLine( 2, "xx|\"yy | zz\",\"aa | bb\"" ),
                new NumberedLine( 3, "|\"xx | yy|aa | bb" ) 
            };

            var caught = false;
            try
            { 
                var actual = tokenizer( inLines ).ToArray();
            }
            catch( Exception ex )
            {
                Assert.IsTrue( ex.Message.Contains( "col 1" ), "column info correct" );
                Assert.IsTrue( ex.Message.Contains( "line 3 " ), "line info correct" );
                Assert.IsTrue( ex.Message.Contains( "\"xx | yy|aa" ), "line echoed" );
                caught = true;
            }

            Assert.IsTrue( caught, "Expected to catch exception." );
        }
    }
}