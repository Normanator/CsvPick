
@echo ...............Viewing header...............
CsvPick -i hereGoes.csv -h
@echo.
@echo expected: 0  Col0
@echo           1  Col1
@echo            ...
@echo           6  Col6

@echo.
@echo ...............Selecting out-of-order fields...............
CsvPick -i hereGoes.csv -f 3,1,4
@echo.
@echo expected: Col3,Col1,Col4
@echo           "troi   ","un","quatre"
@echo           ...etc...


@echo.
@echo ...............LineNumber pseudo-field...............
CsvPick -i hereGoes.csv -f 5,-1,4 -skip 1 -take 2
@echo.
@echo expected: "cinq",3,"quatre"
@echo           'funf',4,'fear'


@echo.
@echo ...............Trim, out-delim, out-file...............
CsvPick -i hereGoes.csv -trim -od \t -o _test0_out.txt
@type _test0_out.txt
@echo.
@echo expected: Col0    Col1    Col2    Col3    Col4    Col5    Col6
@echo           zero    un      deux    troi    quatre  cinq    six
@echo                   einz    zwei    drei    fear    funf
@echo           ...etc...


@echo.
@echo ...............Append, comment, delim...............
@erase _test1_out.txt
Csvpick -i pipey.csv -d ^| -od \t -cmt -- -take 3 -o _test1_out.txt
Csvpick -i pipey.csv -d ^| -od \t -cmt -- -skip 7 -take 2 --append -o _test1_out.txt
@type _test1_out.txt
@echo.
@echo expected: Col0   Col1    Col2    Col3
@echo           L1-0    L1-1    L1-2    L1-3
@echo           L2-0    L2-1    L2-2    L2-3
@echo           L7-0    L7-1    L7-2    L7-3
@echo           L8-0    L8-1    L8-2    L8-3


@echo.
@echo ...............Sampling...............
CsvPick -i pipey.csv -d ^| -pct 50:222 -f 1
@echo.
@echo expected: L1-1
@echo           L3-1
@echo           L5-1
@echo           L6-1


@echo.
@echo ...............Scripting...............
CsvPick -i pipey.csv -d ^| -od , -cmt -- -skip 1 -f -1,3,2,1 --script _script_test2.cs
@echo.
@echo expected:  5,3-1L,2-1L,1-1L
@echo            7,3-3L,2-3L,1-3L
@echo            9,3-5L,2-5L,1-5L
@echo            11,3-7L,2-7L,1-7L


@echo.
@echo ...............PostSkipTake...............
CsvPick -skip -2 -take -3  --script _filter_test.cs -i etc.csv
@echo.
@echo expected:  Female  Cameron 16      John Conner?    10W30
@echo            Female  Sarah   15      Terminators!    MREs
@echo            Female  Ripley  16      Get away from her, you bitch!   Power bars


@echo.
@echo ...............With filter...............
CsvPick -i etc.csv -f 1,2,3 -with 1==16
@echo.
@echo expected:  Cameron 16      John Conner?
@echo            Ripley  16      Get away from her, you bitch!
@echo.
CsvPick -i etc.csv -f 1,2,3 -with 1!=18
@echo.
@echo expected:  Shaggy 13      Zoinks!
@echo            Velma  14      Jinkies!
@echo             ... 
@echo.

@echo.
@echo ............Filter and script with args..
CsvPick -i etc.csv -f 0,1,2 -with 0==Female --script _arg_filt.cs(S) 
@echo.
@echo expected: Female  Velma   14
@echo           Female  Sarah   15
@echo           Female  Uhura   17
@echo.
