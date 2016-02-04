﻿using System;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace ModelBuilder.UnitTests
{
    public class DefaultConstructorResolverTests
    {
        private readonly ITestOutputHelper _output;

        public DefaultConstructorResolverTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void ResolveIgnoresInterfaceAndAbstractParameterTypesWhenSearchingForBestConstructorTest()
        {
            var target = new DefaultConstructorResolver();

            var constructor = target.Resolve(typeof (WithInterfaceAndAbstractParameters));

            constructor.GetParameters().Length.Should().Be(5);
        }

        [Fact]
        public void ResolveMatchesConstructorWithDerivedParameterTypesTest()
        {
            var person = new Person
            {
                Id = Guid.NewGuid()
            };

            var target = new DefaultConstructorResolver();

            var constructor = target.Resolve(typeof (Person), person);

            constructor.GetParameters().Length.Should().Be(1);
        }

        [Fact]
        public void ResolveMatchesConstructorWithMatchingParametersTypesTest()
        {
            var target = new DefaultConstructorResolver();

            var constructor = target.Resolve(typeof (WithValueParameters), "first", "last", DateTime.UtcNow, true,
                Guid.NewGuid(),
                Environment.TickCount);

            constructor.GetParameters().Length.Should().Be(6);
        }

        [Fact]
        public void ResolveReturnsDefaultConstructorOnSimpleModelTest()
        {
            var target = new DefaultConstructorResolver();

            var constructor = target.Resolve(typeof (Simple));

            constructor.GetParameters().Should().BeEmpty();
        }

        [Fact]
        public void ResolveReturnsDefaultConstructorWhenManyConstructorsAvailableTest()
        {
            var target = new DefaultConstructorResolver();

            var constructor = target.Resolve(typeof (WithMixedValueParameters));

            constructor.GetParameters().Should().BeEmpty();
        }

        [Fact]
        public void ResolveReturnsParameterConstructorTest()
        {
            var target = new DefaultConstructorResolver();

            var constructor = target.Resolve(typeof (WithValueParameters));

            constructor.GetParameters().Should().NotBeEmpty();
        }

        [Fact]
        public void ResolveThrowsExceptionWhenNoConstructorMatchingSpecifiedParametersTest()
        {
            var target = new DefaultConstructorResolver();

            Action action = () => target.Resolve(typeof (Simple), Guid.NewGuid().ToString(), true, 123);

            _output.WriteLine(action.ShouldThrow<MissingMemberException>().And.Message);
        }

        [Fact]
        public void ResolveThrowsExceptionWhenNoPublicConstructorFoundTest()
        {
            var target = new DefaultConstructorResolver();

            Action action = () => target.Resolve(typeof (Singleton));

            _output.WriteLine(action.ShouldThrow<MissingMemberException>().And.Message);
        }

        [Fact]
        public void ResolveThrowsExceptionWhenParameterValuesDoNotMatchParameterTypesTest()
        {
            var target = new DefaultConstructorResolver();

            var priority = Convert.ToDouble(Environment.TickCount);

            Action action =
                () =>
                    target.Resolve(typeof (WithValueParameters), "first", "last", DateTime.UtcNow, true, Guid.NewGuid(),
                        priority);

            _output.WriteLine(action.ShouldThrow<MissingMemberException>().And.Message);
        }
    }
}