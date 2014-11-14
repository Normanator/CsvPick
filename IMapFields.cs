using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsvPick
{
    interface IMapFields
    {
        IEnumerable<IEnumerable<string>> Project( IEnumerable<string> fields );
    }
}
