using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CsvPick
{
    class AbstractProcess
    {
        /// <summary>
        /// Derive the line-ending string (LR,CRLF) and encoding (ASCII, UTF8,...)
        /// </summary>
        /// <param name="reader">The input source</param>
        /// <returns>A tuple of line-ending and encoding</returns>
        public Tuple<string,Encoding> GetLineMarkAndEncoding( StreamReader reader )
        {
            return Tuple.Create( "...", Encoding.UTF8 );
        }

        public static Func<TextReader, IEnumerable<NumberedLine>>  CreateLineSource()
        {
            return GenerateLines;
        }

        public static Func<IEnumerable<NumberedLine>, IEnumerable<NumberedLine>> CreateSkipTake( 
                  string  commentMarker,
                  int     skip,
                  int     take,
                  double  samplePercent,
                  int     sampleSeed )
        {
            var sampler = new Sampler( samplePercent, sampleSeed );
            take        = (take <= 0) ? Int32.MaxValue : take;

            Func<IEnumerable<NumberedLine>, IEnumerable<NumberedLine>> filter = (seq) =>
                {
                    var subSeq =
                        seq.Skip( skip )
                           .Where( nl => !nl.Line.StartsWith( commentMarker ) )
                           .SampleFrom( sampler )
                           .Take( take );

                    return subSeq;
                };

            return filter;
        }

        public static Func<IEnumerable<NumberedLine>,IEnumerable<NumberedRecord>> 
            CreateTokenizer( 
                char            delimChar,
                FieldParseType  parseType,
                int []          columns,
                bool            trimQuotes )
        {
            Func<IEnumerable<NumberedLine>, IEnumerable<NumberedRecord>> tokenizer = (lst) =>
              {
                  var df = new DelimFinder( delimChar, parseType );
                  var dp = new DelimParser( df, columns, trimQuotes );
                  var parsed = lst.SelectMany ( nl => dp.ParseMany(nl) );
                  return parsed; 
              };
              
            return tokenizer;
        }

        public static Func<IEnumerable<NumberedRecord>,IEnumerable<NumberedRecord>> 
            CreateProjector( string scriptFile )
        {


        }

        public static Func<IEnumerable<NumberedRecord>,IEnumerable<string>> 
            CreateFormatter(
                string outDelim,
                int [] columns )
        {
            Func<int,string[],string[]>  getSrcFields = (lineNo,fields) => fields;
            if( columns.Any( i => i == -1 ) )
            {
                // Source line-number is treated as an additional field
                var len      = columns.Length;
                columns      = 
                    columns.Select( i => (i >= 0) ? i : (len - 1) ).ToArray();
                getSrcFields = 
                    (n,f) => f.Concat( new [] { n.ToString() } ).ToArray();
            }

            var recFmt = String.Join(
                            outDelim, 
                            columns.Select(o => "{"+ o.ToString() +"}").ToArray() );

            Func<IEnumerable<NumberedRecord>,IEnumerable<string>> formatter = (lst) =>
                {
                    // TODO: Use SelectMany s.t. we can show header fields as seperate 'records'
                    return lst.Select( nr => 
                        {
                            var vals = getSrcFields( nr.LineNumber, nr.Fields );
                            return String.Format( recFmt, vals );
                        } );
                };
                
            return formatter;
        }

        public static Action<TextWriter, IEnumerable<string>> CreateOutputter(
                        string   prependValue,
                        string   endOfLineMarker,
                        bool     prependOutIndex )
        {
            Action<TextWriter, IEnumerable<string>> outputter = ( w, stm ) =>
                {
                    var idx = 0;
                    foreach( var line in stm )
                    {
                        w.Write( prependValue );
                        if( prependOutIndex )
                        {
                            w.Write( idx.ToString().PadRight( 5 ) );
                        }
                        w.Write( line );

                        prependValue = endOfLineMarker;
                    }
                };

            return outputter;
        }


        // -------------------------------
        #region private methods
        private static IEnumerable<NumberedLine>  GenerateLines( TextReader reader )
        {
            int n = 0;
            for(;;)
            {
                var line = reader.ReadLine();
                if( line == null )
                    break;

                ++n;
                yield return new NumberedLine( n, line );
            }
        }

        private static IEnumerable<NumberedRecord> ProcessRecords( 
                          IEnumerable<NumberedRecord> seq )
        {
            // TODO: Are we picking fields, calling scripts? 
        }
        #endregion


    }

    // ----------------------------

    public static class MyExtensions
    {

        public static Func<T,U>  Compose<T,V,U>( Func<V,U> outer, Func<T,V> inner )
        {
            Func<T,U> composed = (t) => outer( inner( t ) );
            return composed;
        }

        public static Action<T1,T2> EndChain<T1,T2,V>( Action<T1,V> terminal, Func<T2,V> inner )
        {
            Action<T1,T2> composed = (t1,t2) => terminal( t1, inner( t2 ) );
            return composed;
        }


        /// <summary>
        /// Chains two function delegates to form a single, new function
        /// </summary>
        /// <typeparam name="T">Input type of first function</typeparam>
        /// <typeparam name="V">Output of first, input of next</typeparam>
        /// <typeparam name="U">Output type of next function</typeparam>
        /// <param name="first">The initial funciton in the chain</param>
        /// <param name="next">The next function in the chain</param>
        /// <returns>A new function: next( first( t ) )</returns>
        public static Func<T,U>  Then<T,V,U>( this Func<T,V> first, Func<V,U> next )
        {
            return Compose<T,V,U>( next, first );
        }
    }


}
