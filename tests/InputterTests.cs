using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CsvPickTests
{
    using CsvPick;


    [TestClass]
    public class InputterTests
    {

        internal static IEnumerable<NumberedLine>  AsNumbLines( IEnumerable<string> strArr )
        {
            int lineNo = 0;
            foreach( var str in strArr )
                yield return new NumberedLine( ++lineNo, str );
        }

        internal static IEnumerable<NumberedRecord>  AsNumbRecs( 
            IEnumerable<Tuple<string,string[]>> strs,
            bool                                echo = false )
        {
            int lineNo = 0;
            foreach( var pair in strs )
            {
                var nr = new NumberedRecord( ++lineNo, pair.Item1, pair.Item2 );
                if( echo )
                {
                    nr = new NumberedRecord( nr, pair.Item2 );
                }
                yield return nr;
            }
        }

        internal static IEnumerable<NumberedRecord>  AddOutFields( 
                 IEnumerable<NumberedRecord>      seq,
                 IEnumerable<IEnumerable<string>> outFields )
        {
            return seq.TZip( outFields )
                      .Select( pair => new NumberedRecord( pair.Item1, pair.Item2 ) );
        }
    }

    // --------------------------------------------

    internal static class NRAssert
    {
        public static void AreEqual( NumberedRecord expected, NumberedRecord actual )
        {
            Assert.AreEqual( expected.LineNumber, actual.LineNumber );
            Assert.AreEqual( expected.Line,       actual.Line );
            Assert.IsTrue( expected.Fields.SequenceEqual( actual.Fields ), "Fields agree" );
            Assert.IsTrue( (expected.OutFields == null) == (actual.OutFields == null) );
            if( expected.OutFields != null )
            { 
                Assert.IsTrue( expected.OutFields.SequenceEqual( actual.OutFields ),
                               "OutFields agree" );
            }
        }

        public static void AreEqual( NumberedLine expected, NumberedLine actual )
        {
            Assert.AreEqual( expected.LineNumber, actual.LineNumber );
            Assert.AreEqual( expected.Line,       actual.Line );
        }

        public static void AreEqual( IEnumerable<NumberedLine> expected, IEnumerable<NumberedLine> actual )
        {
            foreach( var pair in expected.TZip( actual ) )
            {
                NRAssert.AreEqual( pair.Item1, pair.Item2 );
            }
            Assert.AreEqual( expected.Count(), actual.Count() );
        }

        public static void AreEqual( IEnumerable<NumberedRecord> expected, IEnumerable<NumberedRecord> actual )
        {
            foreach( var pair in expected.TZip( actual ) )
            {
                NRAssert.AreEqual( pair.Item1, pair.Item2 );
            }
            Assert.AreEqual( expected.Count(), actual.Count() );
        }

        public static void CollectionEquals<T,U>( IEnumerable<T> expected, IEnumerable<U> actual )
        {
            foreach( var pair in expected.TZip( actual ) )
            {
                Assert.AreEqual( pair.Item1, pair.Item2 );
            }
            Assert.AreEqual( expected.Count(), actual.Count() );
        }

        public static void NestedEquals<T>( IEnumerable<IEnumerable<T>> expected,
                                            IEnumerable<IEnumerable<T>> actual )
        {
            Assert.AreEqual( expected.Count(), actual.Count(), "Outer lengths differ " );
            foreach( var pair in expected.TZip( actual ) )
            {
                Assert.IsTrue( pair.Item1.SequenceEqual( pair.Item2 ) );
            }
        }

        #region Linq extensions

        public static IEnumerable<Tuple<T,U>>  TZip<T,U>( this IEnumerable<T> first, IEnumerable<U> second )
        {
            return first.Zip( second, (f,s) => Tuple.Create( f, s ) );
        }
        #endregion
    }

}
