public class MyPred
{
    public bool Filter( string[] inFields )
    {
        int score    = 0;
        var fieldArr = inFields.ToArray();
        return fieldArr[ 1 ] == "Blue" &&
               !int.TryParse( fieldArr[ 0 ], out score ) ||
               score >= 80;
    }
}