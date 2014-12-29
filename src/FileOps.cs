using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CsvPick;

namespace My.Utilities
{

    public class FileOps
    {

        #region Interpreting files

        public static Tuple<bool,string> EoLFinder( char [] buf )
        {
            int foundAt = Array.IndexOf( buf, '\n' );
            if( foundAt == -1 )
                return Tuple.Create( false, "" );

            var guess = (foundAt > 0 && buf[ foundAt - 1 ] == '\r')
                          ? "\r\n"
                          : "\n";
            return Tuple.Create( true, guess );
        }
        
        
        public static Tuple<bool,char> DelimFinder( char [] buf )
        {
            var guess = buf.FirstOrDefault( c => c==',' || c=='\t' );
            if( guess == default(char) )
                return Tuple.Create( false, '*' );

            return Tuple.Create( true, guess );
        }


        public static T GuessFromStream<T>( System.IO.Stream           stream, 
                                            Func<char[],Tuple<bool,T>> finder,
                                            T                          defaultAns )
        {
            if( !stream.CanSeek )
                return defaultAns;

            T     ans = defaultAns;
            long  pos = stream.Position;
            try
            {
                stream.Seek( 0L, System.IO.SeekOrigin.Begin );
                var reader = new System.IO.StreamReader( stream, Encoding.UTF8, true, 4096 );

                const int bufLen = 512;
                char [] buf      = new char[ bufLen ];
                int     offset   = 0;
                int     ctRead   = bufLen;
                for(int i = 0;  (i < 20) && (ctRead == (bufLen-offset)); ++i )
                {
                    buf[0] = buf[ bufLen - 1 ];
                    ctRead = reader.Read( buf, offset, bufLen - offset );
                    offset = 1;
                    
                    var guess = finder( buf );
                    if( !guess.Item1 )
                        continue;
                    
                    ans = guess.Item2;
                    break;
                }
            }
            finally
            {
                stream.Seek( pos, System.IO.SeekOrigin.Begin );
            }

            return ans;
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


        #region Reader and Writer Creation
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


        public static Tuple<TextReader,string,Encoding,char> OpenInput(
            string   inFile )
        {
            TextReader  reader         = null;
            string      endOfLineMark  = Environment.NewLine;
            char        delimiterGuess = default(char);
            Encoding    encoding       = Encoding.ASCII;

            if( string.IsNullOrEmpty( inFile ) )
            {
                reader          = Console.In;

                //TODO: Detect stdin encoding.  Cmd /U should open Unicode pipes.
            }
            else if( inFile.StartsWith("http:") || inFile.StartsWith("https:") )
            {
                var webreq = System.Net.HttpWebRequest.Create( inFile ) as System.Net.HttpWebRequest;
                webreq.UseDefaultCredentials = true;
                webreq.Headers.Add(System.Net.HttpRequestHeader.AcceptEncoding, "gzip,deflate");
                webreq.Headers.Add("Content-Encoding", "UTF-8");
                webreq.AutomaticDecompression = System.Net.DecompressionMethods.Deflate |
                                                System.Net.DecompressionMethods.GZip;

                var webresp = webreq.GetResponse();  
                //TODO: extract encoding from webresp.Headers(?)  StreamReader?
                encoding    = Encoding.UTF8;
                var fs      = webresp.GetResponseStream();
                reader      = new System.IO.StreamReader( fs, true );
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
                endOfLineMark   = GuessFromStream( fs, EoLFinder, endOfLineMark );
                delimiterGuess  = GuessFromStream( fs, DelimFinder, delimiterGuess );
                reader = sr;
            }

            return Tuple.Create( reader, endOfLineMark, encoding, delimiterGuess );
        }
        #endregion

    } // end class FileOps


}