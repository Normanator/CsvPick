using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsvPick
{
    [System.Diagnostics.DebuggerDisplay("{LineNumber} {DbgLine}")]
    public class NumberedLine
    {
        public NumberedLine( int lineNum, string line )
        {
            this.LineNumber = lineNum;
            this.Line       = line;
        }
    
        public int LineNumber { get; private set; }
        public string Line    { get; private set; }

        public string GetAuditString()
        {
            var line    = this.Line ?? "<null>";
            var len     = line.Length;
            var trimLen = Math.Min( len, 60 );
            var msg = string.Format( "line {0} = (\"{1}...\")",
                             this.LineNumber, 
                             line.Substring( 0, trimLen ) );
            return msg;
        }

        protected virtual string DbgLine
        {
            get
            { 
                var line = Line ?? "<none>";
                var len  = Math.Min( line.Length, 30 );
                var elipsis = (len < line.Length) ? "..." : string.Empty;
                return line.Substring( 0, len ) + elipsis; }
        }
    }

    [System.Diagnostics.DebuggerDisplay("line#={LineNumber} fldCt={DbgFieldCt} {DbgLine}")]
    public class NumberedRecord : NumberedLine
    {
        public NumberedRecord( int lineNum, string line, IEnumerable<string> data )
            : base( lineNum, line )
        {
            this.Fields     = data.ToArray();
        }

        internal NumberedRecord( NumberedLine nl, IEnumerable<string> data )
            : base( nl.LineNumber, nl.Line )
        {
            this.Fields = data.ToArray();
        }

        internal NumberedRecord( NumberedRecord nr, IEnumerable<string> data )
            : this( (NumberedLine) nr, nr.Fields )
        {
            this.OutFields = data.ToArray();
        }

        public string[] Fields     { get; private set; }
        public string[] OutFields  { get; set; }

        #region Debugger display
        private int DbgFieldCt
        {
            get { return Fields == null ? 0 : Fields.Length;  }
        }

        protected override string DbgLine
        {
            get
            { 
                Func<string[],string> pretty = (arr) =>
                    {
                        var sb     = new StringBuilder();
                        var prefix = string.Empty;
                        var suffix = string.Empty;
                        var ct = arr.Length;
                        int i  = 0;
                        while( sb.Length < 45 && i < ct )
                        {
                            var str = arr[ i ];
                            var len = Math.Min( 12, str.Length );
                            suffix  = (len < str.Length) ? "..." : string.Empty;
                            sb.Append( prefix );
                            sb.Append( arr[ i ].Substring( 0, len ) );
                            sb.Append( suffix );
                            prefix = "|";
                            ++i;
                        }
                        return sb.ToString();
                    };

                if( Fields == null )
                    return base.DbgLine;

                return OutFields == null
                    ? "flds=" + pretty( Fields )
                    : "outf=" + pretty( OutFields );
            }
        }
        #endregion
    }

}