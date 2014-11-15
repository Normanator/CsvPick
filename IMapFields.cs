using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsvPick
{
    public interface IMapFields
    {
        /// <summary>
        /// Transforms the input record's fields into 0,1, or more output records.
        /// </summary>
        /// <param name="fields">Extracted input fields</param>
        /// <returns>Output row(s)</returns>
        IEnumerable<IEnumerable<string>> Project( IEnumerable<string> fields );

        FieldsFormatter GetOutFormatter( int [] columns );
    }


    public class BasicMapFields : IMapFields
    {
        public IEnumerable<IEnumerable<string>> Project( IEnumerable<string> fields )
        {
            var lst = new List<IEnumerable<string>>( 1 );
            lst.Add( fields );
            return lst;
        }

        public FieldsFormatter GetOutFormatter( int [] columns )
        {
            return new FieldsFormatter( columns, ',', false );
        }
    }
}
