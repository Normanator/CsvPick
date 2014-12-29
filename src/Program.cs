using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


using My.Utilities;
using System.IO;

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

                RunPipeline( programArguments );

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

                var oldClr = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine( sb.ToString() );
                Console.ForegroundColor = oldClr;
            }

            Done:
            if( System.Diagnostics.Debugger.IsAttached )
            {
                Console.WriteLine( "\r\nHit any key to exit debugging..." );
                Console.ReadKey( true );
            }
            return retCode;
        }


        private static char  GuessInputDelimiter( char argDelim, char detectedDelim )
        {
            var inDelim = (argDelim != default(char))
                            ? argDelim
                            : (detectedDelim != default(char))
                                  ? detectedDelim
                                  : ',';
            return inDelim;
        }


        private static string GuessEndOfLineMark( string detectedEoL, bool forceCRLF )
        {
            return forceCRLF
                      ? "\r\n"
                      : detectedEoL;
        }

        private static Tuple<int[],int[]>  ReduceColumns( int [] columns )
        {
            if( columns == null )
                return Tuple.Create( (int[])null, (int[])null );

            var toExtract = columns.Where( v => v > -1)
                                   .OrderBy (v => v)
                                   .Distinct().ToArray();
            Func<int,int> findIndex = (v) => 
              {
                  if( v == -1 ) return -1;
                  int idx = 0;
                  foreach( var te in toExtract )
                  {
                    if( te == v )
                        return idx;
                
                    ++idx;
                  }
                  throw new ApplicationException( "unable to find element" );
              };
      
            var reduced   = columns.Select ( v => findIndex(v) ).ToArray();
    
            return Tuple.Create( toExtract, reduced );
        }


        private static void RunPipeline( MyProgramArguments progArgs )
        {
            var reader = (System.IO.TextReader)null;
            var writer = (System.IO.TextWriter)null;
            try
            { 
                var input         = FileOps.OpenInput( progArgs.InFile );
                reader            = input.Item1;
                var endOfLineMark = GuessEndOfLineMark( input.Item2,
                                                        progArgs.ForceCRLF );
                var outEncoding   = input.Item3;
                var inDelim       = GuessInputDelimiter( progArgs.Delimiter,
                                                         input.Item4 );
                var outDelim      = progArgs.OutDelimiter != default(char)
                                      ? new string( progArgs.OutDelimiter, 1 )
                                      : new string( inDelim, 1 );

                var take          = progArgs.TakeLines;
                var addOutIndices = false;
                if( progArgs.ShowHeaders )
                {
                    outDelim      = endOfLineMark;
                    take          = 1;
                    addOutIndices = true;
                }

                var output   = FileOps.OpenOutput(
                                    progArgs.OutFile,
                                    outEncoding,
                                    progArgs.Append );
                writer        = output.Item1;
                var preWrite  = output.Item2
                                  ? endOfLineMark
                                  : String.Empty;

                // TODO: Woof, that's a lot of parameters!  Break Compose up a tad.
                var pipeline = ComposePipeline(
                                   inDelim,
                                   progArgs.SkipLines,
                                   take,
                                   progArgs.CommentString,
                                   progArgs.SamplePercent,
                                   progArgs.SampleSeed,
                                   progArgs.FieldParseType,
                                   progArgs.Columns,
                                   outDelim,
                                   progArgs.Trim,
                                   progArgs.ScriptFile,
                                   endOfLineMark,
                                   addOutIndices,
                                   preWrite,
                                   progArgs.ContinueOnError );

                pipeline( writer, reader );
            }
            finally
            {
                if( reader != null )
                    reader.Dispose();
                if( writer != null )
                    writer.Dispose();
            }
        }


        private static Action<TextWriter, TextReader> ComposePipeline( 
                           char            inDelim,
                           int             skip,
                           int             take,
                           string          commentStr,
                           double          samplePct,
                           int             sampleSeed,
                           FieldParseType  fieldParseType,
                           int []          columns,
                           string          outDelim,
                           bool            trim,
                           string          scriptFile,
                           string          endOfLineMark,
                           bool            addOutIndices,
                           string          preWrite,
                           bool            continueOnError )
        {
            Action<Exception> errHandler = null;
            if( continueOnError )
            {
                errHandler = (ex) =>
                {
                    var oldClr = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Error.WriteLine( "Continuing past error: {0}", ex.Message );
                    Exception ix = ex.InnerException;
                    while( ix != null )
                    {
                        Console.Error.WriteLine( "   inner err: {0}", ix.Message );
                        ix = ix.InnerException;
                    }
                    Console.ForegroundColor = oldClr;
                    // Do not rethrow
                };
            }

            var reducedColumns = ReduceColumns( columns ).Item2;


            var getRawLines = AbstractProcess.CreateLineSource();
            var sampleLines = AbstractProcess.CreateSkipTake(
                                 commentStr,
                                 skip,
                                 take,
                                 samplePct,
                                 sampleSeed );
            var tokenize    = AbstractProcess.CreateTokenizer(
                                 inDelim,
                                 fieldParseType,
                                 columns,
                                 trim, 
                                 errHandler);
            var project     = AbstractProcess.CreateProjector(
                                 reducedColumns );

            var inPipe = getRawLines
                             .Then( sampleLines )
                             .Then( tokenize )
                             .Then( project );

            Func<IEnumerable<NumberedRecord>,IEnumerable<NumberedRecord>>       scriptFilter    = null;
            Func<IEnumerable<NumberedRecord>,IEnumerable<IEnumerable<string>>>  scriptTransform = null;
            if( !String.IsNullOrWhiteSpace( scriptFile ) )
            {
                var fs = new FieldScript( scriptFile );
                scriptFilter    = fs.GetFilter( errHandler );
                scriptTransform = fs.GetTransform( errHandler );
            }

            if( scriptFilter != null )
            {
                inPipe = inPipe.Then( scriptFilter );
            }

            var xform       = scriptTransform ??
                              AbstractProcess.CreatePassThruTransform();
            var format      = AbstractProcess.CreateFormatter(
                                 outDelim,
                                 addOutIndices );
            var outputLines = AbstractProcess.CreateOutputter(
                                 preWrite,
                                 endOfLineMark );

            var pipeline = inPipe
                             .Then( xform )
                             .Then( format );
            var process  = MyExtensions.EndChain(
                             outputLines,
                             pipeline );

            return process;
        }

    } // end class Program

    // ---------------------------------------

    internal class MyProgramArguments : My.Utilities.ProgramArguments
    {
        public MyProgramArguments() : base( caseSensitive: false )
        {
            this.HelpSummary = "CsvPick.exe\r\n" + 
                "Allows you to extract a subset of columns from a CSV file.";

            this.Add( new ArgDef( "InFile" )
             { ShortSwitch="i", LongSwitch="inFile", UnSwitched=true,
               HelpText="The input CSV file.\r\nStdin if unspecified.\r\n" +
                        "(BETA: URL accepted. Not streamed, mind the memory.  NTLM auth)" } );

            this.Add( new ArgDef( "OutFile" )
             { ShortSwitch="o", LongSwitch="outFile",
               HelpText="The output CSV file.\r\nStdout if unspecified." } );

            this.Add( new ArgDef( "FieldList" )
             { ShortSwitch="f", LongSwitch="fields",
               HelpText="Comma-seperated list of fields\r\nzero-based indices of the input columns.\r\n" + 
                        "You can also use -1 to echo input line-number." } );

            this.Add( new ArgDef( "Delimiter" ) 
             { ShortSwitch="d", LongSwitch="delimiter", 
               HelpText="The field delimiter character.\r\nA tab can be expressed as \\t.\r\n" +
                        "Default is to pick first of comma or tab encountered in file.\r\n" +
                        "(stdin or URL input, MUST specify -d explicitly)" } );

            this.Add( new ArgDef( "OutDelimiter" ) 
             { ShortSwitch="od", LongSwitch="outDelimiter", 
               HelpText="(optional) Field delimiter to use in output, defaults to Delimiter value" } );

            this.Add( new ArgDef("ShowHeaders")
                { ShortSwitch="h", LongSwitch="headers", ArgKind=ArgDef.Kind.Bool,
                  HelpText="Shows just the first row with ordinals (useful for later -f)" } );

            this.Add( new ArgDef( "SkipLines" )
             { ShortSwitch="skip", LongSwitch="skipLines",
               ArgKind = ArgDef.Kind.Int,  DefaultValue= (object) 0,
               HelpText="How many lines of inFile should be skipped before parsing?" } );

            this.Add( new ArgDef( "TakeLines" )
             { ShortSwitch="take", LongSwitch="takeLines",
               ArgKind = ArgDef.Kind.Int,  DefaultValue= (object) -1,
               HelpText="How many lines of output should be emitted?\r\n" +
                        "(default of -1 means no limit)" } );

            this.Add( new ArgDef( "FieldForm" )
             { ShortSwitch="form", LongSwitch="fieldForm",
               HelpText="Directs the parser to ignore the Delimiter (see /d) within fields forms\r\n" +
                        "   QUOTED (double- and single-quoted strings)\r\n" + 
                        "   JSON   (curly-brace enclosed strings)\r\n" +
                        "(may supply both comma-seperated)" } );

            this.Add( new ArgDef("Trim")
            { ShortSwitch = "trim", LongSwitch = "trim", ArgKind=ArgDef.Kind.Bool,
              HelpText = "Removes surrounding quotes from input fields" } );

            this.Add( new ArgDef( "ContinueOnError" ) 
             { ShortSwitch="c", LongSwitch="continue", ArgKind=ArgDef.Kind.Bool,
               HelpText="Emits errors to stderr but continues at next record" } );

            this.Add( new ArgDef( "CommentString" )
              { ShortSwitch="cmt",  LongSwitch="commentChar", DefaultValue=(object)"#",
                HelpText = "Ignore any input lines beginning with this string" } );

            this.Add( new ArgDef( "Append" )
              { ShortSwitch="a", LongSwitch="append", ArgKind=ArgDef.Kind.Bool,
                HelpText="Appends to output instead of truncating outFile." } );

            this.Add( new ArgDef( "ForceCRLF" )
              { ShortSwitch="dos", LongSwitch="ForceCRLF", ArgKind=ArgDef.Kind.Bool,
                HelpText="Ensure outFile writes line-breaks with CR-LF.\r\n" +
                         "Ordinarily inFile's newline method is used."} );

            this.Add( new ArgDef( "SamplePercent" )
             { ShortSwitch="pct", LongSwitch="percent",
               ArgKind = ArgDef.Kind.String, DefaultValue="100",
               HelpText="Randomly sample values.  -pct 5 => 5%, -pct 1.5:123 1.5% seed=123\r\n" +
                        "Applied upstream of -take." } );

            this.Add( new ArgDef( "ScriptFile" )
              { ShortSwitch="scr",  LongSwitch="script", 
                HelpText = "Name of a script-file to operate on extracted fields.\r\n" + 
                           "It should have one top level class with either a Process(...) or\r\n" +
                           "or MultiProcess(...) method, e.g.\r\n" +
                           "public class MyLogic { \r\n" +
                           "   public IEnumerable<string> Process(IEnumerable<string>)\r\n" +
                           "returning any number of fields per each row, or \r\n" + 
                           "   public IEnumerable<IEnumerable<string>> MultiProcess(...)\r\n" + 
                           "turning 1 input row into 0, 1, or more output rows.\r\n" +
                           "You may also have a custom predicate:\r\n" + 
                           "   public bool Filter( IEnumerable<string> inFields )." } );
        }

        public string InFile
        {
            get { return this["InFile"].GetString(); }
        }

        public string OutFile
        {
            get { return this["OutFile"].GetString(); }
        }

        public int [] Columns
        {
            get
            {
                var fieldList = this.FieldList;
                if( String.IsNullOrWhiteSpace( fieldList ) )
                    return null;

                return fieldList.Split( new [] { ',' },
                                        StringSplitOptions.RemoveEmptyEntries )
                                .Select( v => Int32.Parse( v ) )
                                .ToArray();
            }
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

        public bool Trim
        { get { return this["Trim"].GetBool(); } }

        public char Delimiter
        {
            get
            {
                string delimstr = this["Delimiter"].GetString();

                if( string.CompareOrdinal( delimstr, "\\t" ) == 0 )
                    return '\t';

                return (delimstr != null && delimstr.Length > 0)
                         ? delimstr[ 0 ]
                         : default( char );  // This means try to detect
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
                    : this["TakeLines"].GetInt();
            }
        }

        public double SamplePercent
        {
            get
            {
                var paramval = this[ "SamplePercent" ].GetString();
                return CrackPercentValue( paramval ).Item1;
            }
        }

        public int SampleSeed
        {
            get
            {
                var paramval = this[ "SamplePercent" ].GetString();
                return CrackPercentValue( paramval ).Item2;
            }
        }

        public string CommentString
        {
            get { return this["CommentString"].GetString(); }
        }

        public bool ContinueOnError
        {
            get { return this["ContinueOnError"].GetBool();  }
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

        public string ScriptFile
        { get { return this[ "ScriptFile" ].GetString(); } }

        private Tuple<double,int>  CrackPercentValue( string pctValue )
        {
            var pctPortion  = pctValue;
            var seedPortion = (string) null;
            var seedDelim   = pctValue.IndexOf( ':' );
            if( seedDelim != -1 )
            {
                pctPortion  = pctValue.Substring( 0, seedDelim ).Trim();
                seedPortion = pctValue.Substring( seedDelim + 1 ).Trim();
            }
            double percent = -1.0;
            percent = double.TryParse( pctPortion, out percent ) ? percent : 100.0;

            int seed = -1;
            seed = int.TryParse( seedPortion, out seed ) 
                     ? seed
                     : (int)(DateTime.Now.Ticks & 0xFFFFFFFF);

            return Tuple.Create( percent, seed );
        }
    }
}
