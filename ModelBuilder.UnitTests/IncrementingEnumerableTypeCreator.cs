﻿namespace ModelBuilder.UnitTests
{
    using System;
    using System.Collections.Generic;

    public class IncrementingEnumerableTypeCreator : EnumerableTypeCreator
    {
        public override bool IsSupported(Type type, string referenceName, LinkedList<object> buildChain)
        {
            if (base.IsSupported(type, referenceName, buildChain) == false)
            {
                return false;
            }

            var baseType = type.GenericTypeArguments[0];

            if (baseType.IsNullable())
            {
                return false;
            }

            var generator = new RandomGenerator();

            return generator.IsSupported(baseType);
        }

        protected override object CreateChildItem(Type type, IExecuteStrategy executeStrategy, object previousItem)
        {
            if (previousItem == null)
            {
                return base.CreateChildItem(type, executeStrategy, null);
            }

            // Use a double as the base type then convert later
            var value = Convert.ToDouble(previousItem);

            value++;

            var converted = Convert.ChangeType(value, type);

            return converted;
        }
    }
}