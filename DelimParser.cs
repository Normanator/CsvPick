using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsvPick
{
    public class DelimParser
    {
        private DelimFinder          _finder;
        private bool                 _trimFields;
        private Dictionary<int,int>  _nextColumns;

        public DelimParser( DelimFinder finder, int [] columns = null, bool trim = true )
        {
            this._finder     = finder ?? 
                               new DelimFinder( ',', FieldParseType.Quoted );
            this._trimFields = trim;

            if( columns != null && columns.Length > 0 )
            {
                _nextColumns = new Dictionary<int,int>();

                int curr = -1;
                var cols = columns.OrderBy( v => v ).Distinct().ToArray();
                foreach( var c in cols )
                {
                    _nextColumns[ curr ] = c;
                    curr = c;
                }
                _nextColumns[ curr ] = -1;
            }
        }

        private int NextDesiredColumn( int currColumn )
        {
            if( _nextColumns == null )
                return currColumn + 1;

            return _nextColumns[ currColumn ];
        }

        private IList<string> Pad( IList<string> extracts )
        {
            var len    = extracts.Count;
            var reqLen = (_nextColumns != null)
                            ? _nextColumns.Count - 1
                            : len;
            for( int i = len; i < reqLen; ++i )
            {
                extracts.Add( string.Empty );
            }

            return extracts; 
        }

        public IList<string> Parse( string line )
        {
            char []        inChars  = line.ToCharArray();
            int            len      = inChars.Length;
            int            start    = 0;
            int            end      = -1;
            int            colCur   = 0;
            int            nextCol  = -1;
            IList<string>  extracts = new List<string>();

            try
            {
                for (nextCol = NextDesiredColumn(-1);
                     nextCol >= 0;
                     nextCol = NextDesiredColumn(nextCol))
                {
                    for (; colCur <= nextCol; ++colCur)
                    {
                        start = end + 1;
                        if (start >= len)
                        {
                            break;
                        }

                        end = this._finder.Find(inChars, start).First();

                        if (end == -1)
                        {
                            end = len;
                        };
                    }

                    if (start > end || start >= len)
                        break;

                    var found = new string(inChars, start, end - start);
                    if (this._trimFields)
                    {
                        found = TrimSpaceAndQuotes( found );
                    }
                    extracts.Add(found);
                }
            }
            catch( Exception ex )
            {
                ex.Data["columnNum"] = colCur;
                throw;
            }

            extracts = Pad( extracts );

            return extracts;
        }

        public static string TrimSpaceAndQuotes( string str )
        {
            var clean = str.Trim();
            var end   = clean.Length - 1;

            if( end >= 1 )
            {
                var ch1 = clean[ 0 ];
                var ch2 = clean[ end ];
                if( ch1 == ch2 && (ch1 == '\'' || ch1 == '\"') )
                {
                    clean = clean.Substring( 1, end - 1 ).Trim();
                }
            }

            return clean;
        }

        public NumberedRecord Parse( NumberedLine nline )
        {
            try
            {
                var fields = Parse(nline.Line);
                var nr = new NumberedRecord(nline.LineNumber, nline.Line, fields);

                return nr;
            }
            catch( Exception ex )
            {
                var lineAudit = nline.GetAuditString();
                var msg = string.Format( "Error in col {0}, {1}",
                             (ex.Data["columnNum"] ?? "<?>"),
                             lineAudit );
                throw new ApplicationException( msg, ex );
            }
        }

        public IEnumerable<NumberedRecord>  ParseMany( NumberedLine nline )
        {
            if( !string.IsNullOrEmpty( nline.Line ) )
            {
                yield return Parse( nline );
            }
        }
    }
}
