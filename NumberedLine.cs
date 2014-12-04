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

        protected string DbgLine
        {
            get { var line = Line ?? "<none>"; return line.Substring( 0, 30 ) + "..."; }
        }
    }

    [System.Diagnostics.DebuggerDisplay("{LineNumber} fldCt={DbgFieldCt} {DbgLine}")]
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

        public string[] Fields     { get; private set; }
        public string[] OutFields  { get; set; }

        private int DbgFieldCt
        {
            get { return Fields == null ? 0 : Fields.Length;  }
        }
    }

    // -------------------------------

    class LineNumberer
    {
        public int  LineNumber { get; private set; }
    
        public IEnumerable<NumberedLine>  Map( IEnumerable<string> srcLines )
        {
            LineNumber = 0;
            foreach( var line in srcLines )
            {
                ++LineNumber;
                yield return new NumberedLine( LineNumber, line );
            }
        }
    }

}