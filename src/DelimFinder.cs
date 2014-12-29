using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsvPick
{
    public class DelimFinder
    {
        private char     _delim;
        private EmptyCtx _emptyCtx;


        /// <summary>
        /// Constuctor for DelimFinder
        /// </summary>
        /// <param name="delim">The delimiter character to hunt for</param>
        /// <param name="parseType">Fields-enum of parsing options.</param>
        public DelimFinder( char delim, FieldParseType parseType )
        {
            this._delim = delim;
            this._emptyCtx = new EmptyCtx( 
                (parseType & FieldParseType.Json)   == FieldParseType.Json,
                (parseType & FieldParseType.Quoted) == FieldParseType.Quoted );
        }


        /// <summary>
        /// Finds the index of the next occurrence of the delimiter
        /// within the given raw input-line.
        /// <see cref="DelimFinder"/> constructor.
        /// The startFrom value should be larger than the previous delimiter index.
        /// </summary>
        /// <param name="line">Input line to scan</param>
        /// <param name="startFrom">A starting index to scan from.</param>
        /// <returns></returns>
        public int  Find( string line, int startFrom )
        {
            var stack = new Stack<ICtx>(2);
            ICtx ctx  = _emptyCtx;

            var len   = line.Length;
            var prior = '\0';
            int idx;
            for( idx = startFrom; idx < len; ++idx )
            {
                var ch = line[ idx ];
                var newCtx = ctx.TestForContext( ch, prior );
                if( newCtx != null )
                {
                    stack.Push( ctx );
                    ctx = newCtx;
                    prior = ch;
                    continue;
                }

                if( ctx.TestForExit( ch, prior ) )
                {
                    ctx = stack.Count > 0
                            ? stack.Pop()
                            : _emptyCtx;
                    prior = ch;
                    continue;
                }

                if( stack.Count == 0 && ch == this._delim )
                    return idx;

                prior = ch;
            }

            if( stack.Count > 0 )
            { 
                throw new ArgumentException(
                    "Failed to find closing quote or brace.",
                    "line" );
            }

            return idx + 1;
        }

    }

    // --------------------------

    /// <summary>
    /// Contexts in which delim parsing should be suspended
    /// </summary>
    [Flags]
    public enum FieldParseType
    {
        /// <summary>
        /// Delimiter char is not legal within the value
        /// </summary>
        Plain = 0,

        /// <summary>
        /// Delimiter char may appear within the value,
        /// The field's boundaries are scoped within { ... }
        /// </summary>
        Json = 1,

        /// <summary>
        /// Delimiter may appear within the value,
        /// which is scoped by single- or double-quotes. 
        /// </summary>
        Quoted = 2
    }


    #region Handlers for various contexts in which the delimiter character should not be treated as a delimiter
    interface ICtx
    {
        ICtx  TestForContext( char ch, char prior );
        bool  TestForExit( char ch, char prior );
    }

    class EmptyCtx : ICtx
    {
        private bool _isJson;
        private bool _isQuoted;

        public EmptyCtx ( bool isJson, bool isQuoted )
        {
            this._isJson     = isJson;
            this._isQuoted   = isQuoted;
        }

        public ICtx TestForContext(char ch, char prior)
        {
 	        switch( ch )
            {
                case '{'  : return _isJson   ? new JCtx()  : null;
                case '\"' : return _isQuoted ? new DqCtx() : null;
                case '\'' : return _isQuoted ? new SqCtx() : null; 
                default   : return null;
            }
        }

        public bool TestForExit(char ch, char prior)
        {
 	        return false; 
        }
    }

    class DqCtx : ICtx
    {
        public ICtx TestForContext( char ch, char prior )
        {
            return null; 
        }

        public bool TestForExit( char ch, char prior )
        {
            return ( ch == '\"' && prior != '\\' );
        }
    }

    class SqCtx : ICtx
    {
        public ICtx TestForContext( char ch, char prior )
        {
            return null;
        }

        public bool TestForExit( char ch, char prior )
        {
            return (ch == '\'' && prior != '\\');
        }
    }

    class JCtx : ICtx
    {
        public ICtx TestForContext( char ch, char prior )
        {
            switch( ch )
            {
                case '{'  : return new JCtx();
                case '\"' : return new DqCtx();
                case '\'' : return new SqCtx(); 
                default   : return null;
            }
        }

        public bool TestForExit( char ch, char prior )
        {
            return (ch == '}');
        }
    }
    #endregion

}
