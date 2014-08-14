using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


using My.Utilities;

namespace CsvPick
{

    class Program
    {
        static int Main( string[] args )
        {
            int retCode = 1;
            try
            {
                var programArguments = new MyProgramArguments();

                programArguments.Parse( args );

                if( programArguments.Help )
                {
                    Console.WriteLine( programArguments.GetHelp() );
                    goto Done;
                }

                int [] columns = ToColumnArray( programArguments.FieldList );

                var delimFinder = new DelimFinder( 
                                       programArguments.Delimiter,
                                       programArguments.FieldParseType );

                Func<string,string> postProcess = (s) => s;
                if( programArguments.ShowHeaders )
                {
                    var pp = new FieldsPivot( programArguments.OutDelimiter );
                    postProcess = pp.AsNumberedLines;
                }

                FileOps.ProcessStreams( programArguments.InFile,
                    columns,
                    programArguments.OutFile,
                    delimFinder,
                    programArguments.OutDelimiter,
                    skipLines:        programArguments.SkipLines,
                    takeLines:        programArguments.TakeLines,
                    commentIndicator: programArguments.CommentString,
                    append:           programArguments.Append,
                    forceCRLF:        programArguments.ForceCRLF,
                    postProcess:      postProcess );

                retCode = 0;
            }
            catch( Exception ex )
            {
                Exception     ix = ex;
                StringBuilder sb = new StringBuilder();
                do
                {
                    sb.AppendFormat( "[{0}] - {1}\r\n",
                        ix.GetType().ToString(), ix.Message );

                    ix = ix.InnerException;

                } while( ix != null );
                sb.Append( ex.StackTrace );
                Console.Error.WriteLine( sb.ToString() );
            }

            Done:
            if( System.Diagnostics.Debugger.IsAttached )
            {
                Console.WriteLine( "\r\nDone.  Hit Enter to quit" );
                Console.ReadKey( true );
            }
            return retCode;
        }


        internal static int [] ToColumnArray( string fieldList )
        {
            if( string.IsNullOrWhiteSpace( fieldList ) )
                return null;

            fieldList        = fieldList.Trim();
            string [] tokens = fieldList.Split(
                new char[] { ',' },
                StringSplitOptions.RemoveEmptyEntries );

            var  ids = from token in tokens
                       let id = (int) Convert.ChangeType( token.Trim(), typeof( int ) )
                       where id >= -1
                       select id;

            return ids.ToArray();
        }

    } // end class Program

    // ---------------------------------------

    internal class FieldsPivot
    {
        private char _delim;

        public FieldsPivot( char delim ) {  this._delim = delim; }

        public string AsNumberedLines( string line )
        {
            var fields = line.Split( _delim );
            var nums   = Enumerable.Range(0, fields.Count() );
            var nfs    = fields.Zip( nums, (f,n) => string.Format( "  {0}\t({1})", f, n ) );
            var well   = string.Join( "\r\n", nfs );
            return well;
        }
    }

    // ---------------------------------------

    internal class MyProgramArguments : My.Utilities.ProgramArguments
    {
        public MyProgramArguments() : base( caseSensitive: false )
        {
            this.HelpSummary = "CsvPick.exe\r\n" + 
                "Allows you to extract a subset of columns from a CSV file.\r\n" +
                "(lines beginning with '#' are ignored)";

            this.Add( new ArgDef( "InFile" )
             { ShortSwitch="i", LongSwitch="inFile",
               HelpText="The input CSV file.\r\nStdin if unspecified." } );

            this.Add( new ArgDef( "OutFile" )
             { ShortSwitch="o", LongSwitch="outFile",
               HelpText="The output CSV file.\r\nStdout if unspecified." } );

            this.Add( new ArgDef( "FieldList" )
             { ShortSwitch="f", LongSwitch="fields",
               HelpText="Comma-seperated list of fields\r\nzero-based indices of the input columns.\r\n" + 
                        "You can also use -1 to echo input line-number.  See also /c." } );

            this.Add( new ArgDef( "FieldForm" )
             { ShortSwitch="form", LongSwitch="fieldForm",
               HelpText="Directs the parser to ignore the Delimiter (see /d) within fields forms\r\n" +
                        "   QUOTED (double- and single-quoted strings)\r\n" + 
                        "   JSON   (curly-brace enclosed strings)\r\n" +
                        "(may supply both comma-seperated)" } );

            this.Add( new ArgDef("ShowHeaders")
                { ShortSwitch="h", LongSwitch="headers", ArgKind=ArgDef.Kind.Bool,
                  HelpText="Shows the first row vertically with ordinals, useful for later -f assignment" } );

            this.Add( new ArgDef( "Delimiter" ) 
             { ShortSwitch="d", LongSwitch="delimiter", DefaultValue=",",
               HelpText="The field delimiter character.\r\nA tab can be expressed as \\t." } );

            this.Add( new ArgDef( "OutDelimiter" ) 
             { ShortSwitch="od", LongSwitch="outDelimiter", 
               HelpText="(optional) Field delimiter to use in output, defaults to Delimiter value" } );

            this.Add( new ArgDef( "Append" )
              { ShortSwitch="a", LongSwitch="append", ArgKind=ArgDef.Kind.Bool,
                HelpText="Appends to output instead of truncating outFile." } );

            this.Add( new ArgDef( "ForceCRLF" )
              { ShortSwitch="dos", LongSwitch="ForceCRLF", ArgKind=ArgDef.Kind.Bool,
                HelpText="Ensure outFile writes line-breaks with CR-LF.\r\n" +
                         "Ordinarily inFile's newline method is used."} );

            this.Add( new ArgDef( "SkipLines" )
             { ShortSwitch="skip", LongSwitch="skipLines",
               ArgKind = ArgDef.Kind.Int,  DefaultValue= (object) 0,
               HelpText="How many lines of inFile should be skipped before parsing?" } );

            this.Add( new ArgDef( "TakeLines" )
             { ShortSwitch="take", LongSwitch="takeLines",
               ArgKind = ArgDef.Kind.Int,  DefaultValue= (object) -1,
               HelpText="How many lines of output should be emitted?\r\n" +
                        "(default of -1 means no limit)" } );

            this.Add( new ArgDef( "CommentString" )
              { ShortSwitch="cmt",  LongSwitch="commentChar", DefaultValue=(object)"#",
                HelpText = "Ignore any input lines beginning with this string" } );
        }

        public string InFile
        {
            get { return this["InFile"].GetString(); }
        }

        public string OutFile
        {
            get { return this["OutFile"].GetString(); }
        }

        public string FieldList
        {
            get
            { return ShowHeaders 
                ? null
                : this["FieldList"].GetString();
            }
        }

        public string FieldForm
        {
            get { return this["FieldForm"].GetString(); }
        }

        public char Delimiter
        {
            get
            {
                string delimstr = this["Delimiter"].GetString();

                if( string.CompareOrdinal( delimstr, "\\t" ) == 0 )
                    return '\t';

                return delimstr[ 0 ];
            }
        }

        public char OutDelimiter
        {
            get
            {
                string outDelim = this["OutDelimiter"].GetString();

                // Default is the same as the input-file's Delimiter
                if( string.IsNullOrEmpty( outDelim ) )
                    return this.Delimiter;
                
                if( string.CompareOrdinal( outDelim, "\\t" ) == 0 )
                    return '\t';

                return outDelim[ 0 ];
            }
        }


        public int SkipLines
        {
            get { return this["SkipLines"].GetInt(); }
        }

        public int TakeLines
        {
            get
            { 
                return ShowHeaders 
                    ? 1
                    : this["TakeLines"].GetInt(); }
        }

        public string CommentString
        {
            get { return this["CommentString"].GetString(); }
        }

        public bool ShowHeaders
        {
            get { return this["ShowHeaders"].GetBool(); }
        }

        public bool Append
        {
            get { return this["Append"].GetBool(); }
        }

        public bool ForceCRLF
        {
            get { return this["ForceCRLF"].GetBool(); }
        }

        public FieldParseType FieldParseType
        {
            get
            { 
                var ff = FieldParseType.Plain;
                if( this.FieldForm == null )
                    return ff; 

                if( this.FieldForm.IndexOf("JSON",StringComparison.OrdinalIgnoreCase) > -1 )
                {
                    ff |= FieldParseType.Json;
                }
                if( this.FieldForm.IndexOf("QUOTED",StringComparison.OrdinalIgnoreCase) > -1 )
                {
                    ff |= CsvPick.FieldParseType.Quoted;
                }
                return ff;
            }
        }
    }
}
