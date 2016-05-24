﻿using System;
using System.Diagnostics.CodeAnalysis;
using ModelBuilder.Data;

namespace ModelBuilder
{
    /// <summary>
    /// The <see cref="EmailValueGenerator"/>
    /// class is used to generate strings that should represent an email.
    /// </summary>
    public class EmailValueGenerator : RelativeValueGenerator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EmailValueGenerator"/> class.
        /// </summary>
        public EmailValueGenerator()
            : base(PropertyExpression.Email, null)
        {
        }

        /// <inheritdoc />
        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase",
            Justification = "Email addresses are lower case by convention.")]
        protected override object GenerateValue(Type type, string referenceName, object context)
        {
            var firstName = GetValue(PropertyExpression.FirstName, context);
            var lastName = GetValue(PropertyExpression.LastName, context);
            var domain = Domain;
            var gender = GetValue(PropertyExpression.Gender, context);

            Person person;

            if (string.Equals(gender, "male", StringComparison.OrdinalIgnoreCase))
            {
                person = TestData.NextMale();
            }
            else
            {
                person = TestData.NextFemale();
            }

            if (firstName == null)
            {
                firstName = person.FirstName;
            }

            if (lastName == null)
            {
                lastName = person.LastName;
            }

            if (domain == null)
            {
                domain = person.Domain;
            }

            var email = firstName + "." + lastName + "@" + domain;

            return email.Replace(" ", string.Empty).ToLowerInvariant();
        }

        /// <inheritdoc />
        public override int Priority { get; } = 1000;

        /// <summary>
        /// Gets the domain for the email address.
        /// </summary>
        /// <value>The domain part of the email address.</value>
        protected virtual string Domain => null;
    }
}