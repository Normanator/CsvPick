
public class MyExt
{
    public string[] Process( string[] fields )
    {
        var fieldArr = fields.ToArray();
        var longest  = 0;
        for( int i = 0; i < fieldArr.Length; ++i )
        {
            longest = Math.Max( longest, fieldArr[ i ].Length );
            fieldArr[ i ] = new string( fieldArr[ i ].ToCharArray().Reverse().ToArray() );
        }
        return fieldArr.Concat( new [] { longest.ToString() } ).ToArray();
    }
}