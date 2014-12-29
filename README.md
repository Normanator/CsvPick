CsvPick
=======

A Windows command-line utility for selecting subsets from delimited streams.
CsvPick automates those very common tasks of projecting fields, re-ordering them, 
cleaning off enclosed quotes, harmonizing delimiters, appending to existing data, etc.,
things you could do in 'R' but would rather just stay on the command-line to do.


Arbitrary columns/fields can be selected in any order (and repeated if desired).
If the -f switch is omitted, all fields are echoed.  
Fields may contain quoted- or JSON-values (within which the delimiter is ignored).

Ex.F1  -- Picking fields with the -f switch (note:no whitespace can occur in the list)

                CsvPick -i foo.csv -f 0,4,2,2

Ex.F2  -- Discover what field ordinals are available with the -h switch.

                CsvPick -i foo.csv -h

Ex.F3  -- Add the input-file line-number for each record with field '-1'.  Output to bar.csv

                CsvPick -i foo.csv -f 1,2,5,-1 -o bar.csv

Ex.F4  -- Expect delimiters to appear within quoted fields and JSON fields. Trim quotes on output.

                CsvPick -i foo.csv -form JSON,QUOTED -trim 


You can specify the input delimiter character and optionally change the output delimiter.
For file-system streams, the utility will try to auto-detect which of comma or tab delimiter to use.
(For non-seekable streams such as stdin and http, comma is assumed, you must specify -d otherwise)

Ex.D1 -- Specify a pipe-delim and change it to tab on output.  Append to bar.csv

                CsvPick -i foo.csv -f 1,3,5,7 -d ^| -od \t -a -o bar.csv


The input stream can be a text-file from disk, the stdin pipe, or http.
It can have commented lines, or you can specify how many records to skip and take.
As often headers preceed data, you'll probably use -skip 1 alot. 

Ex.C1  -- Define comment marker as --, skip first 5 non-comment lines, take 1000, continue on errors

               CsvPick -i http://svr/foo.csv -cmt -- -skip 5 -take 1000 -c

Ex.C2  -- Pipe processes

               findstr /i Error *.log | CsvPick -f 0,1,2,6


Script more complex transformations or synthetic column creation.
Just define a public class with a public method named 'Process' matching the required signature.
Write your script to match the order you choose thru -f.  Skip/take occur upstream of your script. 
You can also 'SelectMany' to expand a record to multiple records (or omit the record) in a 'MultiProcess' method.

Ex.S1  -- Transform some fields and add new field.
            //myscript.cs
            public class Brilliant
            {
                public IEnumerable<string> Process( IEnumerable<string> inFields )
                {
                    var lst = new List<string>(5);
                    lst.AddRange( inFields.Select( s => s.ToLower() ) );

                    var maxLen = lst.Max( s => s.Length );
                    lst.Add( maxLen.ToString() );

                    return lst;
                }
            }

            CsvPick -i foo.csv -f 8,2,4,5 --script myscript.cs

Ex.S2 -- Take all 'Male' fields and expand their 'Sports' to seperate records.
            //guysports.cs
            public class GuySports
            {
                public IEnumerable<IEnumerable<string>> MultiProcess( IEnumerable<string> inFields )
                {
                    var lst = inFields.ToList();
                    if( lst[ 3 ] == "Male" )			// Suppress some rows
                    {
                        foreach( var sport in lst[4].Split(';') )  // Expand some rows
                        {
                            var rlst = lst.Take( 3 ).ToList();
                            rlst.Add( sport );
                            yield return rlst;
                        }
                    }
                }
            }

            CsvPick -i foo.csv  --script guysports.cs

Ex.S3  -- As MultiProcess is a bit heavy to just filter, you can also script a Filter function.
          // mypredicate.cs
          public class Foo
          {
              public bool Filter( IEnumerable<string> inFields )
              {
                  return inFields[ 2 ] == "CateorgyX" &&
                         double.Parse( inFields[ 3 ] ) < 36.0;
              }
          }


You can randomly sample large files.
(N.B. for http inputs, each line is still brought down over the wire)

Ex.P1  -- Choose roughly 2.5% of the records.  Can specify a repeatable seed.

               CsvPick -i foo.csv -pct 2.5
               CsvPick -i foo.csv -pct 2.5:6543
