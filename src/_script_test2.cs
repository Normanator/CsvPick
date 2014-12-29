public class Wacky
{
	public IEnumerable<string[]> MultiProcess( string[] fields )
	{
		//Console.WriteLine( "script received:" );
		//foreach( var f in fields )
		//    Console.Write( "  {0}", f );
		//Console.WriteLine();

		var       lineNo   = int.Parse( fields[ 0 ] );
		if( (lineNo & 0x01) == 0x01 )
		{
			yield return fields.Select( s => new string( s.ToCharArray().Reverse().ToArray() ) ).ToArray();
		}
	}
}