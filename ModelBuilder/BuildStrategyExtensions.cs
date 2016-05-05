using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using ModelBuilder.Properties;

namespace ModelBuilder
{
    /// <summary>
    /// The <see cref="Extensions"/>
    /// class provides extension methods for the <see cref="IBuildStrategy"/> interface.
    /// </summary>
    public static class BuildStrategyExtensions
    {
        /// <summary>
        /// Clones the specified builder strategy and returns a compiler.
        /// </summary>
        /// <param name="buildStrategy">The build strategy to create the instance with.</param>
        /// <returns>The new build strategy compiler.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="buildStrategy"/> parameter is null.</exception>
        public static IBuildStrategyCompiler Clone(this IBuildStrategy buildStrategy)
        {
            if (buildStrategy == null)
            {
                throw new ArgumentNullException(nameof(buildStrategy));
            }

            var compiler = new BuildStrategyCompiler
            {
                BuildLog = buildStrategy.BuildLog,
                ConstructorResolver = buildStrategy.ConstructorResolver
            };

            foreach (var executeOrderRule in buildStrategy.ExecuteOrderRules)
            {
                compiler.ExecuteOrderRules.Add(executeOrderRule);
            }

            foreach (var ignoreRule in buildStrategy.IgnoreRules)
            {
                compiler.IgnoreRules.Add(ignoreRule);
            }

            foreach (var typeCreator in buildStrategy.TypeCreators)
            {
                compiler.TypeCreators.Add(typeCreator);
            }

            foreach (var valueGenerator in buildStrategy.ValueGenerators)
            {
                compiler.ValueGenerators.Add(valueGenerator);
            }

            return compiler;
        }

        /// <summary>
        /// Creates an instance of <typeparamref name="T"/> using the specified build strategy.
        /// </summary>
        /// <typeparam name="T">The type of instance to create.</typeparam>
        /// <param name="buildStrategy">The build strategy to create the instance with.</param>
        /// <returns>The new instance.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="buildStrategy"/> parameter is null.</exception>
        public static T Create<T>(this IBuildStrategy buildStrategy)
        {
            if (buildStrategy == null)
            {
                throw new ArgumentNullException(nameof(buildStrategy));
            }

            return buildStrategy.CreateWith<T>();
        }

        /// <summary>
        /// Creates an instance of <typeparamref name="T"/> using the specified build strategy and constructor arguments.
        /// </summary>
        /// <typeparam name="T">The type of instance to create.</typeparam>
        /// <param name="buildStrategy">The build strategy to create the instance with.</param>
        /// <param name="args">The constructor arguments to create the type with.</param>
        /// <returns>The new instance.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="buildStrategy"/> parameter is null.</exception>
        public static T CreateWith<T>(this IBuildStrategy buildStrategy, params object[] args)
        {
            if (buildStrategy == null)
            {
                throw new ArgumentNullException(nameof(buildStrategy));
            }

            return buildStrategy.With<DefaultExecuteStrategy<T>>().CreateWith(args);
        }
        
        /// <summary>
        /// Appends a new <see cref="IgnoreRule"/> to the specified <see cref="IExecuteStrategy{T}"/> using the specified expression.
        /// </summary>
        /// <typeparam name="T">The type of instance that matches the rule.</typeparam>
        /// <param name="buildStrategy">The build strategy to clone.</param>
        /// <param name="expression">The expression that identifies a property on <typeparamref name="T"/></param>
        /// <returns>A cloned build strategy with the new rule.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="buildStrategy"/> parameter is null.</exception>
        /// <exception cref="ArgumentNullException">The <paramref name="expression"/> parameter is null.</exception>
        /// <exception cref="ArgumentException">The <paramref name="expression"/> parameter does not represent a property.</exception>
        /// <exception cref="ArgumentException">The <paramref name="expression"/> parameter does not match a property on the type to generate.</exception>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters",
            Justification = "This type is required in order to support the fluent syntax of call sites.")]
        public static IBuildStrategy Ignoring<T>(this IBuildStrategy buildStrategy,
            Expression<Func<T, object>> expression)
        {
            if (buildStrategy == null)
            {
                throw new ArgumentNullException(nameof(buildStrategy));
            }

            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            var propInfo = GetPropertyInfo(expression);

            if (propInfo == null)
            {
                var message = string.Format(CultureInfo.CurrentCulture,
                    Resources.Error_ExpressionNotPropertyFormat,
                    expression);

                throw new ArgumentException(message);
            }

            var type = typeof(T);
            var typeProperties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);

            if (typeProperties.Any(x => x.DeclaringType == propInfo.DeclaringType && x.PropertyType == propInfo.PropertyType && x.Name == propInfo.Name) == false)
            {
                var message = string.Format(CultureInfo.CurrentCulture,
                    Resources.ExecuteStrategy_ExpressionTargetsWrongType, propInfo.Name, type.FullName);

                throw new ArgumentException(message);
            }

            var rule = new IgnoreRule(type, propInfo.Name);

            return buildStrategy.Clone().Add(rule).Compile();
        }

        private static PropertyInfo GetPropertyInfo<T>(Expression<Func<T, object>> expression)
        {
            PropertyInfo property = null;

            var unaryExpression = expression.Body as UnaryExpression;

            if (unaryExpression != null)
            {
                property = ((MemberExpression)unaryExpression.Operand).Member as PropertyInfo;
            }

            if (property != null)
            {
                return property;
            }

            var memberExpression = expression.Body as MemberExpression;

            if (memberExpression != null)
            {
                return memberExpression.Member as PropertyInfo;
            }

            return null;
        }





        /// <summary>
        /// Populates the instance using the specified build strategy.
        /// </summary>
        /// <typeparam name="T">The type of instance to populate.</typeparam>
        /// <param name="buildStrategy">The build strategy.</param>
        /// <param name="instance">The instance to populate.</param>
        /// <returns>The updated instance.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="buildStrategy"/> parameter is null.</exception>
        public static T Populate<T>(this IBuildStrategy buildStrategy, T instance)
        {
            if (buildStrategy == null)
            {
                throw new ArgumentNullException(nameof(buildStrategy));
            }

            return buildStrategy.With<DefaultExecuteStrategy<T>>().Populate(instance);
        }

        /// <summary>
        /// Returns a new <see cref="IExecuteStrategy{T}"/> for the specified build strategy.
        /// </summary>
        /// <typeparam name="T">The type of execute strategy to return.</typeparam>
        /// <param name="buildStrategy">The build strategy.</param>
        /// <returns>A new execute strategy.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="buildStrategy"/> parameter is null.</exception>
        public static T With<T>(this IBuildStrategy buildStrategy) where T : IExecuteStrategy, new()
        {
            if (buildStrategy == null)
            {
                throw new ArgumentNullException(nameof(buildStrategy));
            }

            var executeStrategy = new T { BuildStrategy = buildStrategy };
            
            return executeStrategy;
        }
    }
}