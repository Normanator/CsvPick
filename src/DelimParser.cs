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
        private int                  _priorSize = 1;

        /// <summary>
        /// Constructor for DelimParser 
        /// that knows how to extract a desired set of columns,
        /// trim them to their essence, and pad missing fields.
        /// </summary>
        /// <param name="finder">Delimiter finder to use</param>
        /// <param name="columns">Desired input columns (null for all)</param>
        /// <param name="trim">Should any enclosing quotes be trimmed</param>
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


        /// <summary>
        /// Tokenizes an input line into the desired columns,
        /// as supplied to the <see cref="DelimParser"/> constructor.
        /// </summary>
        /// <param name="line">Raw input line</param>
        /// <returns>Extracted  fields (with empty strings for missing values)</returns>
        internal IList<string> Parse( string line )
        {
            int            len      = line.Length;
            int            start    = 0;
            int            end      = -1;
            int            colCur   = 0;
            int            nextCol  = -1;
            IList<string>  extracts = new List<string>( _priorSize );

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

                        end = this._finder.Find( line, start );

                        if (end == -1)
                        {
                            end = len;
                        };

                        // review: if we wanted instead to emit blank fields for 
                        // unselected ones, do if colCur < nextCol extracts.Add("") here.
                    }

                    if (start > end || start >= len)
                        break;

                    var trimmed = TrimCruft( line, start, end-1, this._trimFields );
                    var found   = line.Substring( trimmed.Item1, trimmed.Item2 );
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


        private static Tuple<int,int> TrimCruft( string line, int start, int end, bool trimQuotes )
        {
            int  ts          = start;
            int  te          = Math.Min( end, line.Length - 1 );
            char lq          = '\0';
            char rq          = '\0';
            bool lCanAdvance = true;
            bool rCanAdvance = true; 
    
            while( ts < te && (lCanAdvance || rCanAdvance) )
            {
                var lch = line[ ts ];
                var rch = line[ te ];
        
                if( lCanAdvance )
                {
                    if( Char.IsWhiteSpace( lch ) )
                    {
                        ++ts;
                    }
                    else if( trimQuotes && (lch == '\"' || lch == '\'') )
                    {
                            lq          = lch;
                            lCanAdvance = false;
                    }
                    else 
                    {
                        lCanAdvance = false;
                    }
                }
        
                if( rCanAdvance )
                {
                    if( Char.IsWhiteSpace( rch ) )
                    {
                        --te;
                    }
                    else if( trimQuotes && (rch == '\"' || rch == '\'') )
                    {
                        rq          = rch;
                        rCanAdvance = false;
                    }
                    else 
                    {
                        rCanAdvance = false;
                    }
                }
        
                if( lq != '\0' && lq == rq )
                {
                    ++ts;
                    --te;
                    lq = rq = '\0';
                    lCanAdvance = rCanAdvance = true;
                }
            }
            return Tuple.Create( ts, te - ts + 1 );
        }


        public IEnumerable<NumberedRecord>  ParseMany( NumberedLine nline )
        {
            if( !string.IsNullOrEmpty( nline.Line ) )
            {
                yield return Parse( nline );
            }
        }


        #region Private methods
        private int NextDesiredColumn( int currColumn )
        {
            if( _nextColumns == null )
                return currColumn + 1;

            return _nextColumns[ currColumn ];
        }


        private IList<string> Pad( IList<string> extracts )
        {
            // BUG: if columns[] is null, we won't pad this record equal to prior peers.
            var len    = extracts.Count;
            var reqLen = (_nextColumns != null)
                            ? _nextColumns.Count - 1 
                            : len;
            for( int i = len; i < reqLen; ++i )
            {
                extracts.Add( string.Empty );
            }

            if( _priorSize < reqLen )
            {
                _priorSize = reqLen;
            }

            return extracts; 
        }


        private NumberedRecord Parse( NumberedLine nline )
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
                ex.Data[ "lineNum" ] = nline.LineNumber;
                throw new ApplicationException( msg, ex );
            }
        }
        #endregion

    } // end class
}
