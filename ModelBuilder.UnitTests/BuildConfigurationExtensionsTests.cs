﻿namespace ModelBuilder.UnitTests
{
    using System;
    using FluentAssertions;
    using Xunit;

    public class BuildConfigurationExtensionsTests
    {
        [Fact]
        public void CloneReturnsCompilerWithBuildStrategyConfigurationTest()
        {
            var target = new DefaultBuildStrategyCompiler().AddIgnoreRule<Person>(x => x.Address).Compile();

            var actual = target.Clone();

            actual.ConstructorResolver.Should().Be(target.ConstructorResolver);
            actual.TypeCreators.ShouldBeEquivalentTo(target.TypeCreators);
            actual.ValueGenerators.ShouldBeEquivalentTo(target.ValueGenerators);
            actual.IgnoreRules.ShouldBeEquivalentTo(target.IgnoreRules);
            actual.ExecuteOrderRules.ShouldBeEquivalentTo(target.ExecuteOrderRules);
        }

        [Fact]
        public void CloneThrowsExceptionWithNullBuildStrategyTest()
        {
            IBuildStrategy target = null;

            Action action = () => target.Clone();

            action.ShouldThrow<ArgumentNullException>();
        }
    }
}