using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsvPick
{
    public class FieldsFormatter
    {
        private bool   _addLineNumbers;
        private string _formatString;

        public FieldsFormatter( int [] columns, char outDelimiter )
        {
            var mop = MakeOrdinalProjection( columns );
            this._addLineNumbers = (mop.Item1.FirstOrDefault() < 0);
            this._formatString   = mop.Item2.Replace( '|', outDelimiter );;
        }

        public string Format( NumberedRecord nr )
        {
            var ae = _addLineNumbers
                      ? (new [] { nr.LineNumber.ToString() }).Concat( nr.Fields )
                      : nr.Fields;

            var outline = string.Format( _formatString, ae.ToArray() );
            return outline; 
        }


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
    }
}
