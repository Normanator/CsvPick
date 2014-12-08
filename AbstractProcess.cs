using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CsvPick
{
    internal class AbstractProcess
    {
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
                        seq.Where( nl => !nl.Line.StartsWith( commentMarker ) )
                           .Skip( skip )
                           .SampleFrom( sampler )
                           .Take( take );

                    return subSeq;
                };

            return filter;
        }

        public static Func<IEnumerable<NumberedLine>,IEnumerable<NumberedRecord>> 
            CreateTokenizer( 
                char               delimChar,
                FieldParseType     parseType,
                int []             columns,
                bool               trimQuotes,
                Action<Exception>  errHandler )
        {
            var df = new DelimFinder( delimChar, parseType );
            var dp = new DelimParser( df, columns, trimQuotes );

            Func<IEnumerable<NumberedLine>, IEnumerable<NumberedRecord>> tokenizer = 
                (lst) => GenerateParsedLines( dp, errHandler, lst );
              
            return tokenizer;
        }

        public static Func<IEnumerable<NumberedRecord>,IEnumerable<NumberedRecord>> 
            CreateProjector( int []    reducedColumns )
        {
            Func<NumberedRecord,string[]> getSrcFields = nr => nr.Fields;
            if( reducedColumns != null && reducedColumns.Any( i => i == -1 ) )
            {
                // Source line-number is treated as an additional field
                var len      = reducedColumns.Length;
                reducedColumns      = 
                    reducedColumns.Select( i => (i >= 0) ? i : (len - 1) ).ToArray();
                getSrcFields = nr => 
                    nr.Fields.Concat( new [] { nr.LineNumber.ToString() } ).ToArray();
            }

            Func<IEnumerable<NumberedRecord>,IEnumerable<NumberedRecord>> projector = ( lst ) =>
                {
                    return lst.Select( nr => 
                        {
                            var srcFields  = getSrcFields( nr );
                            var projFields = 
                                reducedColumns != null
                                    ? reducedColumns.Select( c => srcFields[ c ] ).ToArray()
                                    : srcFields;

                            nr.OutFields = projFields;
                                
                            return nr;
                        } );
                };

            return projector;
        }


        public static Func<IEnumerable<NumberedRecord>, IEnumerable<IEnumerable<string>>>
            CreateScriptor( 
                string            scriptFile,
                Action<Exception> errHandler )
        {
            IMapFields mapFields = String.IsNullOrWhiteSpace( scriptFile )
                                     ? (IMapFields) new BasicMapFields()
                                     : (IMapFields) new FieldScript( scriptFile );
            Func<IEnumerable<NumberedRecord>,IEnumerable<IEnumerable<string>>> scriptor = 
                ( lst ) => GenerateScriptedFields( mapFields, errHandler, lst );
                //{
                //    return lst.SelectMany( nr => mapFields.Project( nr ) );
                //};

            return scriptor;
        }


        public static Func<IEnumerable<IEnumerable<string>>,IEnumerable<string>> 
            CreateFormatter(
                string   outDelim,
                bool     prependOutIndex)
        {
            Func<int,string,string>  hack = (n,s) => s;
            if( prependOutIndex )
            {
                hack = (n,s) => n.ToString().PadRight(8) + s;
            }
            Func<string[],string>    format = 
                (sa) => String.Join( outDelim, sa );

            Func<IEnumerable<string>,string> adornFormat = ( sa ) =>
                format( sa.mapi( (i,s) => hack( i, s ) ).ToArray() );

            Func<IEnumerable<IEnumerable<string>>,IEnumerable<string>> formatter = (lst) =>
                {
                    return lst.Select( sa => adornFormat( sa ) );
                };
                
            return formatter;
        }

        public static Action<TextWriter, IEnumerable<string>> CreateOutputter(
                        string   prependValue,
                        string   endOfLineMarker )
        {
            Action<TextWriter, IEnumerable<string>> outputter = ( w, stm ) =>
                {
                    foreach( var line in stm )
                    {
                        w.Write( prependValue );
                        w.Write( line );

                        prependValue = endOfLineMarker;
                    }
                };

            return outputter;
        }


        // -------------------------------
        #region private methods
        // Because you are not allowed to yield-return from a lambda, these functions exist
 
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

        private static IEnumerable<NumberedRecord>
            GenerateParsedLines(
               DelimParser                 delimParser,
                Action<Exception>          errHandler,
                IEnumerable<NumberedLine>  seq )
        {
            var outSeq = new List<NumberedRecord>();
            foreach( var nl in seq )
            { 
                outSeq.Clear();
                try
                {
                    // Oh, yeah, you can't yield-return from a try-block either!
                    // ...and so we do this silly, perf-wasting interim-container thing.

                    outSeq.AddRange ( delimParser.ParseMany( nl ) );
                }
                catch( Exception ex )
                {
                    if( errHandler == null )
                        throw;

                    errHandler( ex );
                    outSeq.Clear();
                }

                foreach( var ov in outSeq )
                    yield return ov;
            }
        }

        private static IEnumerable<IEnumerable<string>>
            GenerateScriptedFields(
                    IMapFields                  mapFields,
                    Action<Exception>           errHandler,
                    IEnumerable<NumberedRecord> seq )
        {
            var outSeq = new List<IEnumerable<string>>();
            foreach( var nr in seq )
            { 
                outSeq.Clear();
                try
                {
                    // Oh, yeah, you can't yield-return from a try-block either!
                    // ...and so we do this silly, perf-wasting interim-container thing.

                    outSeq.AddRange( mapFields.Project( nr ) );
                }
                catch( Exception ex )
                {
                    if( errHandler == null )
                        throw;

                    errHandler( ex );
                    outSeq.Clear();
                }

                foreach( var ov in outSeq )
                    yield return ov;
            }
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


        /// <summary>
        /// Chains two functions together.
        /// The outer function takes two arguments, the latter of which is the inner function's output.
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="outer"></param>
        /// <param name="inner"></param>
        /// <returns></returns>
        public static Func<T1,T2,U> Compose<T1,T2,V,U>( Func<T1,V,U> outer, Func<T2,V> inner )
        {
            Func<T1,T2,U> composed = (t1,t2) => outer( t1, inner( t2 ) );
            return composed;
        }


        public static Action<T1,T2> EndChain<T1,T2,V>( Action<T1,V> terminal, Func<T2,V> inner )
        {
            Action<T1,T2> composed = (t1,t2) => terminal( t1, inner( t2 ) );
            return composed;
        }


        /// <summary>
        /// Select with the item index also available.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="src">Input stream</param>
        /// <param name="f">Transformation function taking index and element</param>
        /// <returns>An output stream</returns>
        public static IEnumerable<U> mapi<T,U>( this IEnumerable<T> src, Func<int,T,U> f )
        {
            int idx = -1;
            foreach( var s in src )
            {
                ++idx;
                yield return f( idx, s );
            }
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
