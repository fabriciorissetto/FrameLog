using System;
using System.Data.Entity;

namespace FrameLog
{
    internal class ValuePair
    {
        public readonly object OriginalValue;
        public readonly object NewValue;
        private readonly EntityState state;

        public ValuePair(object originalValue, object newValue, EntityState state)
        {
            OriginalValue = get(originalValue);
            NewValue = get(newValue);
            this.state = state;
        }

        private object get(object value)
        {
            if (value is DBNull)
                return null;
            return value;
        }

        public bool HasChanged
        {
            get
            {
                return state == EntityState.Added
                    || state == EntityState.Deleted
                    || !object.Equals(NewValue, OriginalValue);
            }
        }
    }
}