using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Reflection;

namespace CsvPick
{
    class FieldScript
    {
        #region Script entry-point metadata
        private static readonly Type requiredInType = typeof( string[] );
        private static readonly Type outTypeSingle  = typeof( string[] );
        private static readonly Type outTypeMulti   = typeof( IEnumerable<string[]> );
        private static readonly Type outTypeFilter  = typeof( bool );
        private const string methodNameSingle       = "Process";
        private const string methodNameMulti        = "MultiProcess";
        private const string methodNameFilter       = "Filter";
        #endregion

        private Assembly  scriptAssy;

        // Internals for friend-assembly unit-testability.
        internal dynamic  processInstance;
        internal dynamic  filterInstance;
        internal bool     useMultiProcess;


        /// <summary>
        /// Constructor for binding an external script 
        /// </summary>
        /// <param name="scriptFile">C# file to compile into the pipeline</param>
        public FieldScript( string scriptFile )
        {
            CompileScript( scriptFile );

            BindProcessType( );
        }

        internal FieldScript()
        {
            // intentionally blank
            // For unit-tests to supply processInstance or filterInstance
        }


        /// <summary>
        /// Get the filter-function (if any) to pick input records
        /// </summary>
        /// <param name="errHandler">Callback to handle script exceptions</param>
        /// <returns>A stream predicate function</returns>
        public Func<IEnumerable<NumberedRecord>, IEnumerable<NumberedRecord>>  GetFilter( 
            Action<Exception> errHandler)
        {
            if( this.filterInstance == null )
                return null;

            Func<IEnumerable<NumberedRecord>,IEnumerable<NumberedRecord>> filter = (lst) =>
                lst.Where( r => this.Filter( r, errHandler ) );

            return filter; 
        }


        /// <summary>
        /// Get the transform function (if any) to take input fields to output fields,
        /// and perhaps to map input records to differing number of output records.
        /// </summary>
        /// <param name="errHandler">Callback to handle script exceptions</param>
        /// <returns>A stream select-mapping function</returns>
        public Func<IEnumerable<NumberedRecord>, IEnumerable<string[]>>  GetTransform( 
            Action<Exception> errHandler )
        {
            if( this.processInstance == null )
                return null;

            Func<IEnumerable<NumberedRecord>,IEnumerable<string[]>> xform = null;
            if( !this.useMultiProcess )
            {
                xform = (lst) => lst.Select( (r) => this.SingleProject( r, errHandler ) )
                                    .Where( (r) => r != null );
            }
            else
            { 
                xform = (lst) => lst.SelectMany( (r) => this.MultiProject( r, errHandler ) );
            }

            return xform; 
        }


        #region Project or Flatten

        private Exception ComposeException( NumberedRecord nr,
                                            string         baseMsg,
                                            Exception      innerEx )
        {
            var lineAudit = nr.GetAuditString();
            var msg   = string.Format( "{0} Error in col {1}, {2}",
                                       baseMsg,
                                       (innerEx.Data["columnNum"] ?? "<?>"),
                                       lineAudit );

            // TODO: Kind'a expensive. If --continue was supplied, skip stack-trace
            var stack    = innerEx.StackTrace;
            var lastLine = stack.LastIndexOf( "\r\n" );
            if( lastLine != -1 )
            {
                stack = stack.Substring( 0, lastLine );
            }

            var aex      = new ApplicationException( msg, innerEx );
            aex.Data[ "scriptLoc" ] = stack;
            return aex; 
        }


        private string[] SingleProject( NumberedRecord nr, Action<Exception> errHandler )
        {
            try
            {
                return (string []) this.processInstance.Process( nr.OutFields );
            }
            catch( Exception ex )
            {
                var aex = ComposeException( nr, "Script Project", ex );
                if( errHandler != null )
                {
                    errHandler( aex );
                    return null;
                }
                else
                {
                    throw aex;
                }
            }
        }

        private IEnumerable<string[]> MultiProject( NumberedRecord nr, Action<Exception> errHandler )
        {
            try
            {
                // Materialize the record's expansion in order to force any evaluation exception
                IEnumerable<string[]> resp = this.processInstance.MultiProcess( nr.OutFields );
                return resp.ToArray();
            }
            catch( Exception ex )
            {
                var aex = ComposeException( nr, "Script MultiProject", ex );
                if( errHandler != null )
                {
                    errHandler( aex );
                    return new string[0][];
                }
                else
                {
                    throw aex;
                }
            }
        }


        private bool Filter( NumberedRecord nr, Action<Exception> errHandler )
        {
            try
            {
                return this.filterInstance.Filter( nr.OutFields );
            }
            catch( Exception ex )
            {
                var aex = ComposeException( nr, "Script Filter", ex );
                if( errHandler != null )
                {
                    errHandler( aex );
                    return false;
                }
                else
                {
                    throw aex;
                }
            }
        }
        #endregion


        #region Compile script file
        private void CompileScript( string scriptFile )
        {
            var provOptions    = new Dictionary<string, string>();
            provOptions.Add( "CompilerVersion", "v4.0" );
            var compileParams  = new CompilerParameters();
            compileParams.GenerateInMemory = true;
            compileParams.IncludeDebugInformation = true;
            compileParams.ReferencedAssemblies.Add( "System.Core.dll" );
            compileParams.ReferencedAssemblies.Add(
                Assembly.GetExecutingAssembly().Location );

            string scriptSrc = GetScriptSource( scriptFile );

            using( var csCompiler     = new CSharpCodeProvider( provOptions ) )
            {
                var compileResults =
                    csCompiler.CompileAssemblyFromSource( compileParams, scriptSrc );

                ValidateCompile( compileResults.Errors );

                this.scriptAssy = compileResults.CompiledAssembly;
            }
        }

        private static string GetScriptSource( string scriptFile )
        {
            var obviousUsings = "using System;\r\n" +
                                "using System.Collections.Generic;\r\n" +
                                "using System.Linq;\r\n" + 
                                "#line 1 \"" + scriptFile + "\"\r\n";
            var src = "";
            using( var reader = new System.IO.StreamReader( scriptFile ) )
            {
                src = reader.ReadToEnd();
            }
            return obviousUsings + src;
        }

        private static void ValidateCompile( CompilerErrorCollection errors )
        {
            if( errors.Count <= 0 )
                return;

            var sb = new StringBuilder();
            sb.Append( "Failed compiling supplied script-file:\r\n" );
            foreach( var err in errors )
            {
                sb.Append( err.ToString() );
                sb.AppendLine();
            }
            throw new ApplicationException( sb.ToString() );
        }
        #endregion 


        #region Reflect to find processInstance
        private bool IsMultiProcessType( Type type )
        {
            var methods = type.GetMethods();
            var hasMethod = 
                methods.Any( ( m ) =>
                         m.Name == methodNameMulti &&
                         m.IsPublic &&
                         m.ReturnType == outTypeMulti &&
                         m.GetParameters().FirstOrDefault().ParameterType
                           == requiredInType );
            return hasMethod;
        }

        private bool IsSingleProcessType( Type type )
        {
            var methods = type.GetMethods();
            var hasMethod = 
                methods.Any( ( m ) =>
                         m.Name == methodNameSingle &&
                         m.IsPublic &&
                         m.ReturnType == outTypeSingle &&
                         m.GetParameters().FirstOrDefault().ParameterType
                           == requiredInType );
            return hasMethod;
        }

        private bool IsFilterType( Type type )
        {
            var methods = type.GetMethods();
            var hasMethod = 
                methods.Any( ( m ) =>
                         m.Name == methodNameFilter &&
                         m.IsPublic &&
                         m.ReturnType == outTypeFilter &&
                         m.GetParameters().FirstOrDefault().ParameterType
                           == requiredInType );
            return hasMethod;
        }

        private void BindProcessType()
        {
            var types    = scriptAssy.GetExportedTypes();

            var multiPT  = types.Where( (t) => IsMultiProcessType( t ) )
                                .FirstOrDefault();
            if( multiPT != null )
            {
                this.processInstance = Activator.CreateInstance( multiPT );
                this.useMultiProcess = true;
            }
            else
            {
                var singlePT = types.Where( (t) => IsSingleProcessType( t ) )
                                    .FirstOrDefault();
                if( singlePT != null )
                {
                    this.processInstance = Activator.CreateInstance( singlePT );
                    this.useMultiProcess = false;
                }
            }

            var filtT = types.Where( ( t ) => IsFilterType( t ) )
                             .FirstOrDefault();
            if( filtT != null )
            {
                this.filterInstance = Activator.CreateInstance( filtT );
            }

            if( this.processInstance == null && this.filterInstance == null )
            {
                throw new InvalidOperationException(
                       "Failed finding a type with public Process or MultiProcess " +
                       "method with correct signature" );
            }
        }
        #endregion 

    }
}
