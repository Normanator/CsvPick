public class Wacky
{
	public IEnumerable<IEnumerable<string>> MultiProcess( IEnumerable<string> fields )
	{
		//Console.WriteLine( "script received:" );
		//foreach( var f in fields )
		//    Console.Write( "  {0}", f );
		//Console.WriteLine();

		string [] fieldArr = fields.ToArray();
		var       lineNo   = int.Parse( fieldArr[ 0 ] );
		if( (lineNo & 0x01) == 0x01 )
		{
			yield return fieldArr.Select( s => new string( s.ToCharArray().Reverse().ToArray() ) );
		}
	}
}