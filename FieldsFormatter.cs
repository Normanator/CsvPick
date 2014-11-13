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
        private bool   _passThru;
        private string _delimStr;
        private bool   _trim;

        public FieldsFormatter( int [] columns, char outDelimiter, bool trim )
        {
            this._delimStr = new string( outDelimiter, 1 );
            this._trim     = trim;

            if( columns != null )
            {
                var mop = MakeOrdinalProjection( columns );
                this._addLineNumbers = (mop.Item1.FirstOrDefault() < 0);
                this._formatString   = mop.Item2.Replace( '|', outDelimiter );
            }
            else
            {
                this._passThru = true; 
            }
        }

        public string Format( NumberedRecord nr )
        {
            var fields = nr.Fields;
            if( this._trim )
            {
                fields = fields.Select( (f) => TrimField( f ) ).ToArray();
            }

            if( !this._passThru )
            {
                var ae = _addLineNumbers
                          ? (new[] { nr.LineNumber.ToString() }).Concat( fields )
                          : fields;

                var outline = string.Format( _formatString, ae.ToArray() );
                return outline;
            }
            else
            {
                return string.Join( this._delimStr, fields );
            }
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

        private string TrimField( string field )
        {
            field = field.Trim();
            if( field.Length > 0 )
            {
                var end = field.Length - 1;
                if( (field[0] == '\'' && field[end] == '\'') ||
                    (field[0] == '\"' && field[end] == '\"'))
                {
                    field = field.Substring( 1, end-1 );
                    field = field.Trim();
                }
            }
            return field; 
        }
    }
}
