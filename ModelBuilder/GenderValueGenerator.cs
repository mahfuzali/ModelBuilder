namespace ModelBuilder
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    /// <summary>
    /// The <see cref="GenderValueGenerator"/>
    /// class is used to generate random gender values.
    /// </summary>
    public class GenderValueGenerator : ValueGeneratorMatcher
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GenderValueGenerator"/> class.
        /// </summary>
        public GenderValueGenerator()
            : base(new Regex("Gender|Sex", RegexOptions.Compiled | RegexOptions.IgnoreCase), typeof(string))
        {
        }

        /// <inheritdoc />
        protected override object GenerateValue(Type type, string referenceName, LinkedList<object> buildChain)
        {
            var index = Generator.NextValue(0, 1);

            if (index == 0)
            {
                return "Male";
            }

            return "Female";
        }

        /// <inheritdoc />
        public override int Priority
        {
            get;
        } = 1000;
    }
}