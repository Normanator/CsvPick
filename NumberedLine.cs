using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsvPick
{
    public class NumberedLine
    {
        public NumberedLine( int lineNum, string line )
        {
            this.LineNumber = lineNum;
            this.Line       = line;
        }
    
        public int LineNumber { get; private set; }
        public string Line    { get; private set; }
    }


    public class NumberedRecord : NumberedLine
    {
        public NumberedRecord( int lineNum, string line, IEnumerable<string> data )
            : base( lineNum, line )
        {
            this.Fields     = data.ToArray();
        }

        public string[] Fields     { get; private set; }
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