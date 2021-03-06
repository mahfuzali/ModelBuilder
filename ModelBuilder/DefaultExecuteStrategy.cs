﻿namespace ModelBuilder
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Dynamic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using ModelBuilder.BuildActions;
    using ModelBuilder.TypeCreators;
    using ModelBuilder.ValueGenerators;

    /// <summary>
    ///     The <see cref="DefaultExecuteStrategy{T}" />
    ///     class is used to create types and populate instances.
    /// </summary>
    public class DefaultExecuteStrategy : IExecuteStrategy
    {
        private readonly IBuildHistory _buildHistory;
        private readonly IBuildProcessor _buildProcessor;
        private IBuildConfiguration? _configuration;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DefaultExecuteStrategy" /> class.
        /// </summary>
        public DefaultExecuteStrategy() : this(new BuildHistory(), new DefaultBuildLog(), new BuildProcessor())
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DefaultExecuteStrategy" /> class.
        /// </summary>
        /// <param name="buildHistory">The build history tracker.</param>
        /// <param name="buildLog">The build log.</param>
        /// <param name="buildProcessor">The build processor.</param>
        public DefaultExecuteStrategy(IBuildHistory buildHistory, IBuildLog buildLog, IBuildProcessor buildProcessor)
        {
            _buildHistory = buildHistory ?? throw new ArgumentNullException(nameof(buildHistory));
            Log = buildLog ?? throw new ArgumentNullException(nameof(buildLog));
            _buildProcessor = buildProcessor ?? throw new ArgumentNullException(nameof(buildProcessor));
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">The <paramref name="type" /> parameter is <c>null</c>.</exception>
        /// <exception cref="NotSupportedException">
        ///     No <see cref="IValueGenerator" /> or <see cref="ITypeCreator" /> was found to
        ///     generate a requested type.
        /// </exception>
        /// <exception cref="BuildException">Failed to generate a requested type.</exception>
        public object Create(Type type, params object?[]? args)
        {
            return Build(type, args);
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">The <paramref name="method" /> parameter is <c>null</c>.</exception>
        public object?[]? CreateParameters(MethodBase method)
        {
            method = method ?? throw new ArgumentNullException(nameof(method));

            var parameterInfos = Configuration.ParameterResolver.GetOrderedParameters(Configuration, method).ToList();

            if (parameterInfos.Count == 0)
            {
                return null;
            }

            // Create an ExpandoObject to hold the parameter values as we build them
            // ValueGenerators can use these parameters (expressed as properties) to assist in 
            // building values that are dependent on other values
            IDictionary<string, object?> propertyWrapper = new ExpandoObject();

            _buildHistory.Push(propertyWrapper);

            try
            {
                foreach (var parameterInfo in parameterInfos)
                {
                    var lastContext = _buildHistory.Last;

                    Log.CreatingParameter(parameterInfo, lastContext);

                    // Recurse to build this parameter value
                    var parameterValue = Build(parameterInfo);

                    propertyWrapper[parameterInfo.Name!] = parameterValue;

                    Log.CreatedParameter(parameterInfo, lastContext);
                }
            }
            finally
            {
                _buildHistory.Pop();
            }

            var originalParameters = method.GetParameters();
            var parameterValues = new Collection<object?>();

            // Re-order the parameters back into the order expected by the constructor
            foreach (var parameterInfo in originalParameters)
            {
                var parameterValue = propertyWrapper[parameterInfo.Name!];

                parameterValues.Add(parameterValue);
            }

            var parameters = parameterValues.ToArray();

            return parameters;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">The <paramref name="configuration" /> parameter is <c>null</c>.</exception>
        public void Initialize(IBuildConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">The <paramref name="instance" /> parameter is <c>null</c>.</exception>
        /// <exception cref="NotSupportedException">
        ///     No <see cref="IValueGenerator" /> or <see cref="ITypeCreator" /> was found to
        ///     generate a requested type.
        /// </exception>
        /// <exception cref="BuildException">Failed to generate a requested type.</exception>
        public virtual object Populate(object instance)
        {
            instance = instance ?? throw new ArgumentNullException(nameof(instance));

            var type = instance.GetType();

            try
            {
                var capability = _buildProcessor.GetBuildCapability(this, BuildRequirement.Populate,
                    instance.GetType());

                var populatedInstance = Populate(capability, instance);

                if (populatedInstance == null)
                {
                    var message = string.Format(CultureInfo.CurrentCulture,
                        "The type '{0}' failed to return a non-null value of type '{1}' after populating its properties.",
                        capability.ImplementedByType.FullName, type.FullName);

                    throw new BuildException(message, type, null, null, Log.Output);
                }

                return populatedInstance;
            }
            catch (BuildException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var message = string.Format(CultureInfo.CurrentCulture, "Failed to populate instance of type '{0}'",
                    type.FullName);

                throw new BuildException(message, ex);
            }
        }

        /// <summary>
        ///     Builds a value for the specified type.
        /// </summary>
        /// <param name="type">The type of value to build.</param>
        /// <param name="args">The arguments used to create the value.</param>
        /// <returns>The value created.</returns>
        protected virtual object Build(Type type, params object?[]? args)
        {
            type = type ?? throw new ArgumentNullException(nameof(type));

            try
            {
                var instance = Build(
                    () => GetCreateTypeCapability(type),
                    (capability, items) => capability.CreateType(this, type, items), type, args)!;

                if (instance == null)
                {
                    // The Build method above would have thrown an exception if the build capability could not be identified
                    var capability = GetCreateTypeCapability(type)!;

                    var message = string.Format(CultureInfo.CurrentCulture,
                        "The type '{0}' failed to create a non-null value of type '{1}'",
                        capability.ImplementedByType.FullName, type.FullName);

                    throw new BuildException(message, type, null, null, Log.Output);
                }

                RunPostBuildActions(instance, type);

                return instance;
            }
            catch (BuildException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var message = string.Format(CultureInfo.CurrentCulture, "Failed to create instance of type '{0}'",
                    type.FullName);

                throw new BuildException(message, type, null, BuildChain.Last, Log.Output, ex);
            }
        }

        /// <summary>
        ///     Builds a value for the specified parameter.
        /// </summary>
        /// <param name="parameterInfo">The parameter to build a value for.</param>
        /// <returns>The value created for the parameter.</returns>
        protected virtual object? Build(ParameterInfo parameterInfo)
        {
            parameterInfo = parameterInfo ?? throw new ArgumentNullException(nameof(parameterInfo));

            var instance = Build(
                () => _buildProcessor.GetBuildCapability(this, BuildRequirement.Create,
                    parameterInfo),
                (capability, items) => capability.CreateParameter(this, parameterInfo, items),
                parameterInfo.ParameterType, null);

            if (instance == null)
            {
                return instance;
            }

            RunPostBuildActions(instance, parameterInfo);

            return instance;
        }

        /// <summary>
        ///     Builds a value for the specified property.
        /// </summary>
        /// <param name="propertyInfo">The property to build a value for.</param>
        /// <param name="args">The arguments used to create the parent instance.</param>
        /// <returns>The value created for the property.</returns>
        protected virtual object? Build(PropertyInfo propertyInfo, params object?[]? args)
        {
            propertyInfo = propertyInfo ?? throw new ArgumentNullException(nameof(propertyInfo));

            var instance = Build(
                () => _buildProcessor.GetBuildCapability(this, BuildRequirement.Create,
                    propertyInfo),
                (capability, items) => capability.CreateProperty(this, propertyInfo, items), propertyInfo.PropertyType,
                args);

            if (instance == null)
            {
                return instance;
            }

            RunPostBuildActions(instance, propertyInfo);

            return instance;
        }

        /// <summary>
        ///     Populates the specified property on the provided instance.
        /// </summary>
        /// <param name="propertyInfo">The property to populate.</param>
        /// <param name="instance">The instance being populated.</param>
        /// <param name="args">The arguments used to create <paramref name="instance" />.</param>
        protected virtual void PopulateProperty(PropertyInfo propertyInfo, object instance,
            params object?[]? args)
        {
            propertyInfo = propertyInfo ?? throw new ArgumentNullException(nameof(propertyInfo));

            instance = instance ?? throw new ArgumentNullException(nameof(instance));

            if (propertyInfo.GetSetMethod() != null)
            {
                // We can assign to this property
                Log.CreatingProperty(propertyInfo, instance);

                var propertyValue = Build(propertyInfo, args);

                propertyInfo.SetValue(instance, propertyValue, null);

                Log.CreatedProperty(propertyInfo, instance);

                return;
            }

            // The property is read-only
            // Because of prior filtering, we should have a property that is a reference type that we can populate
            var existingValue = propertyInfo.GetValue(instance, null);

            if (existingValue == null)
            {
                // We don't have a value to work with
                return;
            }

            var capability = _buildProcessor.GetBuildCapability(this, BuildRequirement.Populate,
                existingValue.GetType());

            Populate(capability, existingValue, args);

            // This object was never created here but was populated
            // Run post build actions against it so that they can be applied against this existing instance
            RunPostBuildActions(existingValue, propertyInfo);
        }

        private void AutoPopulateInstance(object instance, object?[]? args)
        {
            var propertyResolver = Configuration.PropertyResolver;
            var type = instance.GetType();

            var propertyInfos = propertyResolver.GetOrderedProperties(Configuration, type);

            foreach (var propertyInfo in propertyInfos)
            {
                if (propertyResolver.IsIgnored(Configuration, instance, propertyInfo, args))
                {
                    Log.IgnoringProperty(propertyInfo, instance);
                }
                else
                {
                    PopulateProperty(propertyInfo, instance);
                }
            }
        }

        private object? Build(Func<IBuildCapability> getCapability,
            Func<IBuildCapability, object?[]?, object?> buildInstance, Type type, params object?[]? args)
        {
            var capability = getCapability();

            var context = _buildHistory.Last;

            Log.CreatingType(type, capability.ImplementedByType, context);

            try
            {
                var instance = buildInstance(capability, args);

                if (instance == null)
                {
                    return null;
                }

                if (capability.SupportsPopulate == false
                    && instance.GetType() != type)
                {
                    // Get the capability again on the instance type
                    // The scenario here is that an instance has been created with a different type
                    // where the build action didn't support populating the original type
                    // It has however created a different type that may support population
                    // The example here is IEnumerable<T> which may be built as something like List<T>
                    capability = _buildProcessor.GetBuildCapability(this,
                        BuildRequirement.Create,
                        instance.GetType());
                }

                // Populate the properties
                instance = Populate(capability!, instance, args);

                return instance;
            }
            finally
            {
                Log.CreatedType(type, context);
            }
        }

        private IBuildCapability GetCreateTypeCapability(Type type)
        {
            // As an internal implementation, we know that implementations like EnumerableTypeCreator and ArrayTypeCreator 
            // will make many calls to get a build capability for the same type and state of the build chain.
            // We can increase performance if we can cache the build capabilities for the same requested type for the same item
            // at the end of the build chain
            var capability = _buildHistory.GetCapability(type);

            if (capability != null)
            {
                // We have already calculated this capability
                return capability;
            }

            capability = _buildProcessor.GetBuildCapability(this, BuildRequirement.Create, type);

            _buildHistory.AddCapability(type, capability);

            return capability;
        }

        private object Populate(IBuildCapability capability, object instance, params object?[]? args)
        {
            if (capability.SupportsPopulate == false)
            {
                return instance;
            }

            _buildHistory.Push(instance);
            Log.PopulatingInstance(instance);

            try
            {
                if (capability.AutoPopulate)
                {
                    // The type creator has indicated that this type should be auto populated by the execute strategy
                    AutoPopulateInstance(instance, args);
                }

                // Allow the type creator to do its own population of the instance
                return capability.Populate(this, instance);
            }
            finally
            {
                Log.PopulatedInstance(instance);
                _buildHistory.Pop();
            }
        }

        private void RunPostBuildActions(object instance, Type type)
        {
            var postBuildActions = Configuration.PostBuildActions
                ?.Where(x => x.IsMatch(_buildHistory, type)).OrderByDescending(x => x.Priority);

            if (postBuildActions != null)
            {
                foreach (var postBuildAction in postBuildActions)
                {
                    Log.PostBuildAction(type, postBuildAction.GetType(), instance);

                    postBuildAction.Execute(_buildHistory, instance, type);
                }
            }
        }

        private void RunPostBuildActions(object instance, ParameterInfo parameterInfo)
        {
            var postBuildActions = Configuration.PostBuildActions
                ?.Where(x => x.IsMatch(_buildHistory, parameterInfo)).OrderByDescending(x => x.Priority);

            if (postBuildActions != null)
            {
                foreach (var postBuildAction in postBuildActions)
                {
                    Log.PostBuildAction(parameterInfo.ParameterType, postBuildAction.GetType(), instance);

                    postBuildAction.Execute(_buildHistory, instance, parameterInfo);
                }
            }
        }

        private void RunPostBuildActions(object instance, PropertyInfo propertyInfo)
        {
            var postBuildActions = Configuration.PostBuildActions
                ?.Where(x => x.IsMatch(_buildHistory, propertyInfo)).OrderByDescending(x => x.Priority);

            if (postBuildActions != null)
            {
                foreach (var postBuildAction in postBuildActions)
                {
                    Log.PostBuildAction(propertyInfo.PropertyType, postBuildAction.GetType(), instance);

                    postBuildAction.Execute(_buildHistory, instance, propertyInfo);
                }
            }
        }

        /// <inheritdoc />
        public IBuildChain BuildChain => _buildHistory;

        /// <inheritdoc />
        public IBuildConfiguration Configuration
        {
            get
            {
                if (_configuration == null)
                {
                    var message = string.Format(
                        CultureInfo.CurrentCulture,
                        "The {0} has not be initialized. You must invoke {1} first to provide the build configuration.",
                        GetType().FullName,
                        nameof(Initialize));

                    throw new InvalidOperationException(message);
                }

                return _configuration;
            }
        }

        /// <inheritdoc />
        public IBuildLog Log { get; }
    }
}