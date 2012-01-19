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

                FileOps.ProjectFields( programArguments.InFile,
                    columns,
                    programArguments.OutFile,
                    programArguments.Delimiter,
                    skipLines: programArguments.SkipLines,
                    append:    programArguments.Append,
                    forceCRLF: programArguments.ForceCRLF );
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
                return 1;
            }

            Done:
            if( System.Diagnostics.Debugger.IsAttached )
            {
                Console.WriteLine( "\r\nDone.  Hit Enter to quit" );
                Console.ReadKey( true );
            }
            return 0;
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
                       where id >= 0
                       orderby id
                       select id;

            ids = ids.Distinct();

            return ids.ToArray();
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
             { ShortSwitch="i", LongSwitch="inFile",
               HelpText="The input CSV file.\r\nStdin if unspecified." } );

            this.Add( new ArgDef( "OutFile" )
             { ShortSwitch="o", LongSwitch="outFile",
               HelpText="The output CSV file.\r\nStdout if unspecified." } );

            this.Add( new ArgDef( "FieldList" )
             { ShortSwitch="f", LongSwitch="fields",
               HelpText="Comma-seperated list of fields\r\nzero-based indices.\r\nSee /c." } );

            this.Add( new ArgDef( "Delimiter" ) 
             { ShortSwitch="d", LongSwitch="delimiter", DefaultValue=",",
               HelpText="The field delimiter character.\r\nA tab can be expressed as \\t." } );

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
               HelpText="How many lines of inFile should be skipped before parsing?" +
                        "\r\nNOT YET IMPLEMENTED" } );

            this.Add( new ArgDef( "ShowColumns" )
              { ShortSwitch="c", LongSwitch="columns",
                HelpText="Shows the columns of inFile\r\nTerminates after summary" +
                        "\r\nNOT YET IMPLEMENTED" } );

            this.Add( new ArgDef( "CommentString" )
              { ShortSwitch="x",  LongSwitch="commentChar",
                HelpText = "Ignore any input lines beginning with this string"  +
                        "\r\nNOT YET IMPLEMENTED" } );
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
            get { return this["FieldList"].GetString(); }
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

        public int SkipLines
        {
            get { return this["SkipLines"].GetInt(); }
        }

        public string CommentString
        {
            get { return this["CommentString"].GetString(); }
        }

        public bool ShowColumns
        {
            get { return this["ShowColumns"].GetBool(); }
        }

        public bool Append
        {
            get { return this["Append"].GetBool(); }
        }

        public bool ForceCRLF
        {
            get { return this["ForceCRLF"].GetBool(); }
        }
    }
}
