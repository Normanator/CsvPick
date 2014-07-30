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

        public DelimFinder( char delim, FieldParseType parseType )
        {
            this._delim = delim;
            this._emptyCtx = new EmptyCtx( 
                (parseType & FieldParseType.Json)   == FieldParseType.Json,
                (parseType & FieldParseType.Quoted) == FieldParseType.Quoted );
        }

        public IEnumerable<int>  Find( IEnumerable<char> charseq, int startFrom )
        {
            var stack = new Stack<ICtx>();
            stack.Push( _emptyCtx );

            var chars = charseq.Skip( startFrom );
            var prior = '\0';
            var idx   = -1; 
            foreach( var ch in chars )
            {
                ++idx;
                var ctx    = stack.Peek();
                var newCtx = ctx.TestForContext( ch, prior );
                if( newCtx != null )
                {
                    stack.Push( newCtx );
                    prior = ch;
                    continue;
                }

                if( ctx.TestForExit( ch, prior ) )
                {
                    stack.Pop();
                    prior = ch;
                    continue;
                }

                if( stack.Count == 1 && ch == this._delim )
                    yield return startFrom + idx;

                prior = ch;
            }

            if( stack.Count > 1 )
            { 
                throw new ArgumentException(
                    "Failed to find closing quote or brace.",
                    "line" );
            }

            yield return startFrom + idx + 1;
        }

        public IEnumerable<int>  Find( string line )
        {
            return Find( line.ToCharArray(), 0 );
        }
    }

    // --------------------------

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
}
