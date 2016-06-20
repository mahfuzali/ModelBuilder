﻿namespace ModelBuilder.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using FluentAssertions;
    using ModelBuilder.Data;
    using Xunit;

    public class EmailValueGeneratorTests
    {
        [Fact]
        public void GenerateReturnsDomainFromDerivedClassTest()
        {
            var person = new Person();
            var buildChain = new LinkedList<object>();

            buildChain.AddFirst(person);

            var target = new MailinatorEmailValueGenerator();

            var actual = (string)target.Generate(typeof(string), "email", buildChain);

            actual.Should().EndWith("mailinator.com");
        }

        [Fact]
        public void GenerateReturnsDomainFromTestDataTest()
        {
            var person = new Person();
            var buildChain = new LinkedList<object>();

            buildChain.AddFirst(person);

            var target = new EmailValueGenerator();

            var actual = (string)target.Generate(typeof(string), "email", buildChain);

            var domain = actual.Substring(actual.IndexOf("@") + 1);

            TestData.People.Any(x => x.Domain.ToLowerInvariant() == domain).Should().BeTrue();
        }

        [Fact]
        public void GenerateReturnsEmailAddressWithNameSpacesRemovedTest()
        {
            var person = new Person
            {
                FirstName = "De Jour",
                LastName = "Mc Cormick"
            };
            var buildChain = new LinkedList<object>();

            buildChain.AddFirst(person);

            var target = new EmailValueGenerator();

            var actual = (string)target.Generate(typeof(string), "email", buildChain);

            var expected = "dejour.mccormick";

            actual.Should().StartWith(expected.ToLowerInvariant());
        }

        [Fact]
        public void GenerateReturnsFirstAndLastNameRelativeToFemaleGenderTest()
        {
            var person = new Person
            {
                Gender = Gender.Female
            };
            var buildChain = new LinkedList<object>();

            buildChain.AddFirst(person);

            var target = new EmailValueGenerator();

            var actual = (string)target.Generate(typeof(string), "email", buildChain);

            var firstName = actual.Substring(0, actual.IndexOf("."));

            TestData.Females.Any(x => x.FirstName.ToLowerInvariant() == firstName).Should().BeTrue();
        }

        [Fact]
        public void GenerateReturnsFirstAndLastNameRelativeToMaleGenderTest()
        {
            var person = new Person
            {
                Gender = Gender.Male
            };
            var buildChain = new LinkedList<object>();

            buildChain.AddFirst(person);

            var target = new EmailValueGenerator();

            var actual = (string)target.Generate(typeof(string), "email", buildChain);

            var firstName = actual.Substring(0, actual.IndexOf("."));

            TestData.Males.Any(x => x.FirstName.ToLowerInvariant() == firstName).Should().BeTrue();
        }

        [Fact]
        public void GenerateReturnsRandomEmailAddressTest()
        {
            var firstPerson = new Person
            {
                FirstName = "De Jour",
                LastName = "Mc Cormick"
            };
            var firstBuildChain = new LinkedList<object>();

            firstBuildChain.AddFirst(firstPerson);

            var target = new EmailValueGenerator();

            var first = target.Generate(typeof(string), "email", firstBuildChain);

            first.Should().BeOfType<string>();
            first.As<string>().Should().NotBeNullOrWhiteSpace();
            first.As<string>().Should().Contain("@");

            var secondPerson = new Person
            {
                FirstName = "Sam",
                LastName = "Johns"
            };
            var secondBuildChain = new LinkedList<object>();

            secondBuildChain.AddFirst(secondPerson);

            var second = target.Generate(typeof(string), "email", secondBuildChain);

            first.Should().NotBe(second);
        }

        [Fact]
        public void GenerateReturnsRandomEmailAddressUsingFirstAndLastNameOfContextTest()
        {
            var person = new Person
            {
                FirstName = Guid.NewGuid().ToString("N"),
                LastName = Guid.NewGuid().ToString("N")
            };
            var buildChain = new LinkedList<object>();

            buildChain.AddFirst(person);

            var target = new EmailValueGenerator();

            var actual = (string)target.Generate(typeof(string), "email", buildChain);

            var expected = person.FirstName + "." + person.LastName;

            actual.Should().StartWith(expected.ToLowerInvariant());
        }

        [Fact]
        public void GenerateReturnsRandomEmailAddressUsingFirstOfContextTest()
        {
            var person = new Person
            {
                FirstName = Guid.NewGuid().ToString("N")
            };
            var buildChain = new LinkedList<object>();

            buildChain.AddFirst(person);

            var target = new EmailValueGenerator();

            var actual = (string)target.Generate(typeof(string), "email", buildChain);

            var expected = person.FirstName.Substring(0, 1);

            actual.Should().StartWith(expected.ToLowerInvariant());
        }

        [Fact]
        public void GenerateReturnsRandomEmailAddressUsingLastNameOfContextTest()
        {
            var person = new Person
            {
                LastName = Guid.NewGuid().ToString("N")
            };
            var buildChain = new LinkedList<object>();

            buildChain.AddFirst(person);

            var target = new EmailValueGenerator();

            var actual = (string)target.Generate(typeof(string), "email", buildChain);

            var expected = person.LastName;

            actual.Should().Contain(expected.ToLowerInvariant());
        }

        [Fact]
        public void GenerateReturnsValueWhenContextLacksNameAndGenderPropertiesTest()
        {
            var model = new SlimModel();
            var buildChain = new LinkedList<object>();

            buildChain.AddFirst(model);

            var target = new EmailValueGenerator();

            var actual = (string)target.Generate(typeof(string), "email", buildChain);

            actual.Should().NotBeNullOrWhiteSpace();
        }

        [Theory]
        [InlineData(typeof(Stream), "email")]
        [InlineData(typeof(string), null)]
        [InlineData(typeof(string), "")]
        [InlineData(typeof(string), "Stuff")]
        public void GenerateThrowsExceptionWithInvalidParametersTest(Type type, string referenceName)
        {
            var target = new EmailValueGenerator();

            Action action = () => target.Generate(type, referenceName, null);

            action.ShouldThrow<NotSupportedException>();
        }

        [Fact]
        public void GenerateThrowsExceptionWithNullBuildChainTest()
        {
            var target = new EmailValueGeneratorWrapper();

            Action action = () => target.RunNullTest();

            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void GenerateThrowsExceptionWithNullContextTest()
        {
            var target = new EmailValueGenerator();

            Action action = () => target.Generate(typeof(string), "email", null);

            action.ShouldThrow<NotSupportedException>();
        }

        [Fact]
        public void GenerateThrowsExceptionWithNullTypeTest()
        {
            var target = new EmailValueGenerator();

            Action action = () => target.Generate(null, null, null);

            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void HasHigherPriorityThanStringValueGeneratorTest()
        {
            var target = new EmailValueGenerator();
            var other = new StringValueGenerator();

            target.Priority.Should().BeGreaterThan(other.Priority);
        }

        [Theory]
        [InlineData(typeof(Stream), "email", false)]
        [InlineData(typeof(string), null, false)]
        [InlineData(typeof(string), "", false)]
        [InlineData(typeof(string), "Stuff", false)]
        [InlineData(typeof(string), "email", true)]
        public void IsSupportedTest(Type type, string referenceName, bool expected)
        {
            var person = new Person();
            var buildChain = new LinkedList<object>();

            buildChain.AddFirst(person);

            var target = new EmailValueGenerator();

            var actual = target.IsSupported(type, referenceName, buildChain);

            actual.Should().Be(expected);
        }

        [Fact]
        public void IsSupportedThrowsExceptionWithNullContextTest()
        {
            var target = new EmailValueGenerator();

            var actual = target.IsSupported(typeof(string), "email", null);

            actual.Should().BeFalse();
        }

        [Fact]
        public void IsSupportedThrowsExceptionWithNullTypeTest()
        {
            var person = new Person();
            var buildChain = new LinkedList<object>();

            buildChain.AddFirst(person);

            var target = new EmailValueGenerator();

            Action action = () => target.IsSupported(null, null, buildChain);

            action.ShouldThrow<ArgumentNullException>();
        }

        private class EmailValueGeneratorWrapper : EmailValueGenerator
        {
            public void RunNullTest()
            {
                GenerateValue(typeof(string), "Email", null);
            }
        }
    }
}