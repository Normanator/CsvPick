
public class MyExt
{
    public string[] Process( string[] fields )
    {
        var fieldArr = fields.ToArray();

        if( fieldArr[ 0 ] == "ick" )
        { 
            var aex = new ApplicationException( "Error: ick encountered!" );
            aex.Data[ "columnNum" ] = 123;
            throw aex;
        }

        return fieldArr; 
    }
}