using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CsvPick;

namespace My.Utilities
{

    public class  FileOps
    {

        public static string ChooseCsvColumns( string        input,
                                               DelimFinder   delimFinder = null,
                                               char          outDelim = ',',
                                               string        formatString = null,
                                               params int [] columns )
        {
            if( input == null || columns == null || columns.Length <=0 )
                return input;

            formatString = formatString ?? 
                           string.Join( outDelim.ToString(),
                                        Enumerable.Range( 0, columns.Length )
                                                  .Select( v => "{"+v.ToString()+"}" ) );

            char []        inChars = input.ToCharArray();
            int            len     = inChars.Length;
            delimFinder = delimFinder ?? new DelimFinder( ',', FieldParseType.Quoted );

            int            start    = 0;
            int            end      = -1;
            int            colCur   = 0;
            int            tgtIdx   = 0;
            var            extracts = Enumerable.Repeat( string.Empty, columns.Length ).ToArray();

            for( ; tgtIdx < columns.Length; ++tgtIdx )
            {
                int  colTgt    = columns[ tgtIdx ];
                for( ; colCur <= colTgt; ++colCur )
                {
                    start = end + 1;
                    if( start >= len )
                    {
                        break;
                    }

                    //end   = Array.IndexOf( inChars, delim, start );
                    end = delimFinder.Find( inChars, start ).First();

                    if( end == -1 )
                    {
                        end = len;
                    };
                }

                if( start < end )
                { 
                    var found = new string( inChars, start, end - start );
                    if( !char.IsWhiteSpace(outDelim) )
                    {
                        found = found.Trim();
                    }
                    extracts[ tgtIdx ] = found;
                }

            } // end for tgtIdx

            return string.Format( formatString, extracts );
        }



        public static IEnumerable<string>   ChooseCsvColumns( IEnumerable<string> inputs,
                                                              DelimFinder         delimFinder = null,
                                                              char                outDelim = ',',
                                                              params int []       columns )
        {
            var ordProj      = MakeOrdinalProjection( columns );
            var formatString = ordProj.Item2.Replace( '|', outDelim );

            foreach( string input in inputs )
            {
                yield return ChooseCsvColumns( input, delimFinder, outDelim, formatString, ordProj.Item1 );
            }
        }



        public static IEnumerable<string>  LinesOf( TextReader reader )
        {
            for(;;)
            {
                string line = reader.ReadLine( );
                if( null == line )
                    break;

                yield return line;
            }
        }


        #region Interpreting files

        public static string GuessEndOfLineMark( System.IO.FileStream  fs, string defaultEoL )
        {
            if( !fs.CanSeek )
                return defaultEoL;

            string eol = defaultEoL;
            long   pos = fs.Position;
            try
            {
                fs.Seek( 0L, System.IO.SeekOrigin.Begin );
                var reader = new System.IO.StreamReader( fs, Encoding.UTF8, true, 4096 );

                char [] buf    = new char[ 512 ];
                int     offset = 0;
                for(int i = 0;  i < 20; ++i )
                {
                    int     ctRead = reader.Read( buf, offset, 512 - offset );
                    offset = 1;

                    int     foundAt = Array.IndexOf( buf, '\n' );
                    if( foundAt == -1 )
                        continue;

                    eol = (foundAt > 0 && buf[ foundAt - 1 ] == '\r')
                            ? "\r\n"
                            : "\n";
                    break;
                }
            }
            finally
            {
                fs.Seek( pos, System.IO.SeekOrigin.Begin );
            }

            return eol;
        }



        public static bool StreamEndsWithNewLine( System.IO.Stream stream )
        {
            if( !stream.CanSeek )
                return false;

            long pos = stream.Position;

            try
            {
                long filesize = stream.Seek( 0L, SeekOrigin.End );
                if( filesize == 0 )
                    return true;

                // Unicode endian-ness could be 0A00 or 000A
                // Probably not definitive, but
                // most of my files (ascii, utf-8, or utf-16) should work
                long ctToTest = Math.Min( 2L, filesize );

                stream.Seek( -1L * ctToTest, SeekOrigin.End );
                while( ctToTest-- > 0L )
                {
                    byte b = (byte) stream.ReadByte();
                    if( b == 0x0A )
                        return true;
                }
                return false;
            }
            finally
            {
                stream.Seek( pos, System.IO.SeekOrigin.Begin );
            }
        }

        #endregion

        #region
        /// <summary>
        /// Given an array of field indices returns a tuple of 
        /// sorted, distinct indices (to efficiently read) and a 
        /// formatting string (to easily write).
        /// The input columns can have gaps, repeats, and be un-ordered.
        /// The format string will be zero-based and monotonic s.t. the
        /// read fields array can be printed with it.  
        /// </summary>
        /// <param name="columns">An array of integers</param>
        /// <returns>A tuple of sorted, unique integers and a format-string.</returns>
        public static Tuple<int[],string>  MakeOrdinalProjection( int [] columns )
        {
            var ordered = columns.OrderBy( v => v ).Distinct().ToArray();
            var dict    = new Dictionary<int,int>();
    
            for( int idx = 0; idx < ordered.Length; ++idx )
            {
                dict[ ordered[idx] ] = idx;
            }

            var fmtSb     = new StringBuilder();
            var delimTemp = string.Empty; 
            var ordinals  = columns.Select( v => dict[v] );
            foreach( var ord in ordinals )
            {
                fmtSb.AppendFormat("{0}{{{1}}}", delimTemp, ord );
                delimTemp = "|";
            }

            return Tuple.Create( ordered, fmtSb.ToString() );
        }
        #endregion


        public static void ProjectFields( 
                        string      inFile,
                        int []      columns,
                        string      outFile,
                        DelimFinder delimFinder      = null,
                        char        outDelim         = ',',
                        int         skipLines        = 0,
                        int         takeLines        = -1,
                        string      commentIndicator = "#",
                        bool        append           = false,
                        bool        forceCRLF        = false )
        {
            Encoding              outEncoding     = Encoding.UTF8;
            string                endOfLineMark   = Environment.NewLine;
            string                prewrite        = string.Empty;

            System.IO.TextReader  reader = null;
            System.IO.TextWriter  writer = null;

            try
            {
                var inPair = OpenInput( inFile );
                reader        = inPair.Item1;
                endOfLineMark = inPair.Item2;
                outEncoding   = inPair.Item3;


                var outPair = OpenOutput( outFile, outEncoding, append );
                writer   = outPair.Item1;
                prewrite = outPair.Item2
                            ? endOfLineMark
                            : string.Empty;

                var lines = from line in FileOps.LinesOf( reader ).Skip( skipLines )
                            where !line.StartsWith( commentIndicator )
                            select line;

                int ctOut = 0;
                foreach( string outline in ChooseCsvColumns( lines, delimFinder, outDelim, columns ) )
                {
                    if( ctOut >= takeLines && takeLines >= 0 )
                        break;

                    writer.Write( prewrite );
                    writer.Write( outline );

                    prewrite = endOfLineMark;

                    ++ctOut;
                }
            }
            finally
            {
                if( writer != null ) { writer.Dispose(); }
                if( reader != null ) { reader.Dispose(); }
            }
        }


        public static Tuple<TextWriter,bool> OpenOutput(
            string   outFile,
            Encoding outEncoding,
            bool     append = false )
        {
            TextWriter  writer       = null;
            bool        needsNewline = false;

            if( string.IsNullOrEmpty( outFile ) )
            {
                writer          = Console.Out;
            }
            else
            {
                System.IO.FileMode  openMode = append
                        ? System.IO.FileMode.OpenOrCreate
                        : System.IO.FileMode.Create;

                var fs = new System.IO.FileStream( outFile,
                            openMode,
                            System.IO.FileAccess.ReadWrite,
                            System.IO.FileShare.ReadWrite,
                            8192 );
                if( append && fs.CanSeek )
                {
                    var altstream = new FileStream( outFile,
                        FileMode.Open, FileAccess.Read, FileShare.ReadWrite );
                    using( var sr = new StreamReader( altstream, outEncoding, true ) )
                    {
                        sr.Read();
                        outEncoding = sr.CurrentEncoding;
                    }

                    fs.Seek( 0L, System.IO.SeekOrigin.End );
                }
                writer       = new System.IO.StreamWriter( fs, outEncoding );
                needsNewline = append && !StreamEndsWithNewLine( fs );
            }

            return Tuple.Create( writer, needsNewline );
        }


        public static Tuple<TextReader,string,Encoding> OpenInput(
            string   inFile )
        {
            TextReader  reader        = null;
            string      endOfLineMark = Environment.NewLine;
            Encoding    encoding      = Encoding.ASCII;

            if( string.IsNullOrEmpty( inFile ) )
            {
                reader          = Console.In;

                //TODO: Detect stdin encoding.  Cmd /U should open Unicode pipes.
            }
            else
            {
                var fs =new System.IO.FileStream( inFile,
                            System.IO.FileMode.Open,
                            System.IO.FileAccess.Read,
                            System.IO.FileShare.ReadWrite,
                            32767 );

                StreamReader sr = new System.IO.StreamReader( fs, true );

                encoding        = sr.CurrentEncoding;
                endOfLineMark   = GuessEndOfLineMark( fs, endOfLineMark );
                reader = sr;
            }

            return Tuple.Create( reader, endOfLineMark, encoding );
        }

    }


}