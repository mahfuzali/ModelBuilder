﻿namespace ModelBuilder
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using ModelBuilder.Data;

    /// <summary>
    /// The <see cref="CityValueGenerator"/>
    /// class is used to generate random city values.
    /// </summary>
    public class CityValueGenerator : ValueGeneratorMatcher
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CityValueGenerator"/> class.
        /// </summary>
        public CityValueGenerator()
            : base(new Regex("City", RegexOptions.Compiled | RegexOptions.IgnoreCase), typeof(string))
        {
        }

        /// <inheritdoc />
        protected override object GenerateValue(Type type, string referenceName, LinkedList<object> buildChain)
        {
            var person = TestData.NextPerson();

            return person.City;
        }

        /// <inheritdoc />
        public override int Priority
        {
            get;
        } = 1000;
    }
}