﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Reflection;

namespace CsvPick
{
    class FieldScript : IMapFields
    {
        #region Script entry-point metadata
        private static readonly Type requiredInType = typeof( IEnumerable<string> );
        private static readonly Type outTypeSingle  = typeof( IEnumerable<string> );
        private static readonly Type outTypeMulti   = typeof( IEnumerable<IEnumerable<string>> );
        private const string methodNameSingle       = "Process";
        private const string methodNameMulti        = "MultiProcess";
        #endregion

        private Assembly scriptAssy;
        private dynamic  processInstance;
        private bool     useMultiProcess;

        public FieldScript( string scriptFile )
        {
            CompileScript( scriptFile );

            BindProcessType( );
        }


        public IEnumerable<IEnumerable<string>> Project( IEnumerable<string> fields )
        {
            var lst  = (IEnumerable<IEnumerable<string>>)null;

            if( this.useMultiProcess )
            {
                lst = this.processInstance.MultiProcess( fields )
                         as IEnumerable<IEnumerable<string>>;
            }
            else // Script returns 1 record for each input record.  Wrap it.
            {
                var wrapper = new List<IEnumerable<string>>();
                var results = this.processInstance.Process( fields )
                                as IEnumerable<string>;
                if( results != null )
                {
                    wrapper.Add( results );
                    lst = wrapper;
                }
            }
            return lst ?? new List<IEnumerable<string>>();
        }


        public FieldsFormatter GetOutFormatter( int [] columns )
        {
            var addLineNumbers = columns.Any( i => (i == -1) );
            return new FieldsFormatter( addLineNumbers );
        }


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

            var csCompiler     = new CSharpCodeProvider( provOptions );
            var compileResults =
                csCompiler.CompileAssemblyFromFile( compileParams, scriptFile );

            ValidateCompile( compileResults.Errors );

            this.scriptAssy = compileResults.CompiledAssembly;
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

        private void BindProcessType()
        {
            var types    = scriptAssy.GetExportedTypes();

            var multiPT  = types.Where( (t) => IsMultiProcessType( t ) )
                                .FirstOrDefault();

            if( multiPT != null )
            {
                this.processInstance = Activator.CreateInstance( multiPT );
                this.useMultiProcess = true;
                return;
            }

            var singlePT = types.Where( (t) => IsSingleProcessType( t ) )
                                .FirstOrDefault();
            if( singlePT != null )
            {
                this.processInstance = Activator.CreateInstance( singlePT );
                this.useMultiProcess = false;
                return;
            }

            throw new InvalidOperationException(
                   "Failed finding a type with public Process or MultiProcess " +
                   "method with correct signature" );
        }
        #endregion 

    }
}
