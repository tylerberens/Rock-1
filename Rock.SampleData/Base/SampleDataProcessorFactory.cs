// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Rock.SampleData
{
    /// <summary>
    /// A factory that is responsible for creating processors for sample data actions.
    /// </summary>
    public class SampleDataProcessorFactory
    {
        private Dictionary<SampleDataChangeAction, Type> _ActionProcessorMappings = null;

        public List<SampleDataChangeAction> GetActions()
        {
            // Get all of the SampleDataActionProvider classes in the loaded assemblies.

            if ( _ActionProcessorMappings == null )
            {
                RegisterAllInCurrentApplication();
            }

            return _ActionProcessorMappings.Keys.ToList();
        }

        private void EnsureInitialized()
        {
            if ( _ActionProcessorMappings == null )
            {
                RegisterAllInCurrentApplication();
            }
        }

        public ISampleDataFactory GetProcessorForAction( string id )
        {
            EnsureInitialized();

            var keyAction = _ActionProcessorMappings.Keys.FirstOrDefault( x => x.Key != null && x.Key.Equals( id, StringComparison.OrdinalIgnoreCase ) );

            Type processorType = null;

            if ( _ActionProcessorMappings.ContainsKey( keyAction ) )
            {
                processorType = _ActionProcessorMappings[keyAction];
            }

            ISampleDataFactory processor = null;

            if ( processorType != null )
            {
                processor = Activator.CreateInstance( processorType ) as ISampleDataFactory;
            }

            return processor;
        }

        public void RegisterAllInCurrentApplication()
        {
            _ActionProcessorMappings = new Dictionary<SampleDataChangeAction, Type>();

            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where( a => !a.IsDynamic ).ToList();

                foreach ( var assembly in assemblies )
                {
                    try
                    {
                        Trace.TraceInformation( "Scanning Assembly \"{0}\"...", assembly.FullName );

                        // Get the components that provide Sample Data Change Actions.
                        var processorTypes = assembly.GetExportedTypes()
                                                     .Where( t => t.GetInterfaces().Contains( typeof( ISampleDataFactory ) )
                                                                  && !t.IsAbstract )
                                                                  //&& !t.ContainsGenericParameters )
                                                     .ToList();

                        if ( processorTypes.Any() )
                        {
                            Trace.TraceInformation( "Registering Action Processors..." );

                            foreach ( var processorType in processorTypes )
                            {
                                try
                                {
                                    this.RegisterActionProcessor( processorType );
                                }
                                catch ( Exception ex )
                                {
                                    // If an individual Processor fails to register, send errors to Trace and continue processing other Types in the assembly.
                                    Trace.TraceError( ex.Message );
                                }
                            }
                        }
                    }
                    catch ( Exception ex )
                    {
                        // Send Errors to Trace and continue.
                        Trace.TraceError( ex.Message );
                    }
                }

            }
            catch ( Exception ex )
            {
                throw new Exception( "ActionProcessor registration failed. Failed to load one or more assemblies in current AppDomain.", ex );
            }
        }
       
        /// <summary>
        /// Registers all of the sample data actions provided by the processor of the specified type.
        /// </summary>
        /// <param name="processorType"></param>
        public void RegisterActionProcessor( Type processorType )
        {
            try
            {
                Trace.TraceInformation( "Registering Action Processor \"{0}\"...", processorType.FullName );

                // Create a default instance of the Processor.
                var processor = Activator.CreateInstance( processorType ) as ISampleDataFactory;

                if ( processor == null )
                {
                    return;
                }

                var processorName = processorType.FullName;

                var actions = new List<SampleDataChangeAction>();

                actions.AddRange( processor.GetActionList() );

                // If the processor does not support any Actions, ignore it.
                // This will occur for processors that are intended for internal use only.
                if ( !actions.Any() )
                {
                    return;
                }

                foreach ( var action in actions )
                {
                    var actionKey = action.Key;

                    string prefixedActionCode = processorName + "::" + actionKey;

                    if ( _ActionProcessorMappings.Keys.Any(x => x.Key == prefixedActionCode ) )
                    {
                        throw new Exception( string.Format(
                                                            "ActionProcessor registration failed. Processor \"{0}\" failed to register ActionTypeCode \"{1}\" because it conflicts with an existing registration.",
                                                            processorName, prefixedActionCode ) );
                    }

                    action.Key = prefixedActionCode;

                    _ActionProcessorMappings.Add( action, processorType );

                    //if ( _ActionProcessorMappings.ContainsKey( actionCode ) )
                    //{
                    //    // If the ActionCode already exists without a prefix, remove it to avoid confusion with the ActionCode we are adding now.
                    //    _ActionProcessorMappings.Remove( actionCode );
                    //}
                    //else
                    //{
                    //    // Add the ActionCode without a prefix to allow it to be identified without the namespace.
                    //    _ActionProcessorMappings.Add( action, processorType );
                    //}
                }
            }
            catch ( Exception ex )
            {
                throw new Exception( string.Format( "Action Processor Registration failed for \"{0}\".", processorType.FullName ), ex );
            }
        }
    }

    /// <summary>
    /// A component that is capable of adding, removing, or modifying sample data in a Rock database.
    /// </summary>
    public interface ISampleDataFactory
    {
        List<SampleDataChangeAction> GetActionList();

        SampleDataChangeActionExecutionResponse ExecuteAction( string actionId, ISampleDataChangeActionSettings settings, ITaskMonitorHandle monitor );
    }
}
