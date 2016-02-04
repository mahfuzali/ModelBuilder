﻿using System;

namespace ModelBuilder
{
    /// <summary>
    /// The <see cref="IValueGenerator"/>
    /// interface defines the members for generating values.
    /// </summary>
    /// <remarks>
    /// Values generated by <see cref="IValueGenerator"/> are different to types created by <see cref="ITypeCreator"/> in that they do not have their properties set after construction.
    /// Value types and immutable types (strings for example) should use <see cref="IValueGenerator"/> to create them rather than <see cref="ITypeCreator"/>.
    /// </remarks>
    public interface IValueGenerator
    {
        /// <summary>
        /// Generates a new value of the specified type.
        /// </summary>
        /// <param name="type">The type of value to generate.</param>
        /// <returns>A new value of the type.</returns>
        object Generate(Type type);

        /// <summary>
        /// Returns whether the specified type is supported by this generator.
        /// </summary>
        /// <param name="type">The type to evaulate.</param>
        /// <returns><c>true</c> if the type is supported; otherwise <c>false</c>.</returns>
        bool IsSupported(Type type);
    }
}