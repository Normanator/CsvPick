using System;
using System.Collections.Generic;
using System.Linq;


public class MyThing
{
  public IEnumerable<string> Process( IEnumerable<string> fields )
  {
    var fieldArr = fields.ToArray();
    var name    = fieldArr[ 0 ];
    var saying  = fieldArr[ 1 ];
    saying = new string( saying.ToCharArray().Reverse().ToArray() );
    var codes   = fieldArr[ 2 ];
    var gender  = fieldArr[ 3 ];
    gender      = (string.Compare(gender, "male", true) == 0)
                     ? "Boy"
                     : "Girl";
    var favFood = fieldArr[ 4 ];
    favFood     = string.IsNullOrWhiteSpace(favFood)
                     ? "Scooby Snacks!"
                     : favFood;
    return new [] { name, saying, codes, gender, favFood };                 
  }

  // -------------------------

  public IEnumerable<IEnumerable<string>> MultiProcess( IEnumerable<string> fields )
  {
    var fieldArr = fields.ToArray();
    var name    = fieldArr[ 0 ];
    var saying  = fieldArr[ 1 ];
    saying = new string( saying.ToCharArray().Reverse().ToArray() );
    var codes   = fieldArr[ 2 ].Split( ';' );
    codes       = !codes.Any() ? new [] {""} : codes;
    var gender  = fieldArr[ 3 ];
    gender      = (string.Compare(gender, "male", true) == 0)
                     ? "Boy"
                     : "Girl";
    var favFood = fieldArr[ 4 ];
    favFood     = string.IsNullOrWhiteSpace(favFood)
                     ? "Scooby Snacks!"
                     : favFood;

    foreach( var code in codes )
    {
        yield return new [] { name, saying, code, gender, favFood };
    }
  }

}