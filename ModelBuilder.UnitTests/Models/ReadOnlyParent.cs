﻿namespace ModelBuilder.UnitTests
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    public class ReadOnlyParent
    {
        public ReadOnlyParent()
        {
            Company = new Company();
            RestrictedPeople = EmptySet();
            AssignablePeople = new List<Person>();
            People = new Collection<Person>();
            ReadOnlyPerson = new Person();
            PrivateValue = 0;
        }

        private IEnumerable<Person> EmptySet()
        {
            yield break;
        }

        public IEnumerable<Person> AssignablePeople { get; private set; }

        public Company Company { get; private set; }

        public ICollection<Person> People { get; private set; }
        public int PrivateValue { get; }

        public Person ReadOnlyPerson { get; }

        public IEnumerable<Person> RestrictedPeople { get; private set; }

        public Person Unassigned { get; private set; }
    }
}