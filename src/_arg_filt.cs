public class FilterWithArg
{
	private string _minName = "";

	public FilterWithArg( string minName )
	{ this._minName = minName; }

	public bool Filter( string[] inFields )
	{
		return string.Compare( inFields[1], _minName, ignoreCase: true ) >= 0;
	}
}