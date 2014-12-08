
public class MyExt
{
    public IEnumerable<IEnumerable<string>> MultiProcess( IEnumerable<string> fields )
    {
        var fieldArr = fields.ToArray();
        if( !fieldArr[0].StartsWith("ick") )
        {
            var parts = fieldArr[ 1 ].Split( '/' );
            foreach( var part in parts )
            {
                yield return new[] { part, fieldArr[ 2 ] };
            }
        }
    }
}