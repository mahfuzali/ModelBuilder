﻿namespace ModelBuilder.UnitTests
{
    using System;
    using System.IO;
    using FluentAssertions;
    using Xunit;

    public class PostCodeValueGeneratorTests
    {
        [Fact]
        public void GenerateReturnsRandomValueTest()
        {
            var target = new PostCodeValueGenerator();

            var first = target.Generate(typeof(string), "zip", null);

            first.Should().BeOfType<string>();
            first.As<string>().Should().NotBeNullOrWhiteSpace();

            var second = target.Generate(typeof(string), "zip", null);

            first.Should().NotBe(second);
        }

        [Fact]
        public void GenerateReturnsValueForPostCodeTypeTest()
        {
            var target = new PostCodeValueGenerator();

            var actual = target.Generate(typeof(string), "Zip", null);

            actual.Should().BeOfType<string>();
        }

        [Theory]
        [InlineData(typeof(string), "postcode", true)]
        [InlineData(typeof(string), "PostCode", true)]
        [InlineData(typeof(string), "zip", true)]
        [InlineData(typeof(string), "Zip", true)]
        [InlineData(typeof(string), "zipCode", true)]
        [InlineData(typeof(string), "ZipCode", true)]
        [InlineData(typeof(string), "zipcode", true)]
        [InlineData(typeof(string), "Zipcode", true)]
        public void GenerateReturnsValuesForSeveralNameFormatsTest(Type type, string referenceName, bool expected)
        {
            var target = new PostCodeValueGenerator();

            var actual = (string)target.Generate(type, referenceName, null);

            actual.Should().NotBeNullOrEmpty();
        }

        [Theory]
        [InlineData(typeof(Stream), "postcode")]
        [InlineData(typeof(string), null)]
        [InlineData(typeof(string), "Stuff")]
        public void GenerateThrowsExceptionWithInvalidParametersTest(Type type, string referenceName)
        {
            var target = new PostCodeValueGenerator();

            Action action = () => target.Generate(type, referenceName, null);

            action.ShouldThrow<NotSupportedException>();
        }

        [Fact]
        public void GenerateThrowsExceptionWithNullTypeTest()
        {
            var target = new PostCodeValueGenerator();

            Action action = () => target.Generate(null, null, null);

            action.ShouldThrow<ArgumentNullException>();
        }

        [Theory]
        [InlineData(typeof(Stream), "postcode", false)]
        [InlineData(typeof(string), null, false)]
        [InlineData(typeof(string), "", false)]
        [InlineData(typeof(string), "Stuff", false)]
        [InlineData(typeof(string), "postcode", true)]
        [InlineData(typeof(string), "PostCode", true)]
        [InlineData(typeof(string), "zip", true)]
        [InlineData(typeof(string), "Zip", true)]
        [InlineData(typeof(string), "zipCode", true)]
        [InlineData(typeof(string), "ZipCode", true)]
        [InlineData(typeof(string), "zipcode", true)]
        [InlineData(typeof(string), "Zipcode", true)]
        public void IsSupportedTest(Type type, string referenceName, bool expected)
        {
            var target = new PostCodeValueGenerator();

            var actual = target.IsSupported(type, referenceName, null);

            actual.Should().Be(expected);
        }

        [Fact]
        public void IsSupportedThrowsExceptionWithNullTypeTest()
        {
            var target = new PostCodeValueGenerator();

            Action action = () => target.IsSupported(null, null, null);

            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void PriorityReturnsPositiveValueTest()
        {
            var target = new PostCodeValueGenerator();

            target.Priority.Should().BeGreaterThan(0);
        }
    }
}