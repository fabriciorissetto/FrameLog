using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Objects;
using System.Linq;
using FrameLog.Models;
using System.Data.Entity;

namespace FrameLog.Logging
{
    public class Recorder<TChangeSet, TPrincipal> : IOven<TChangeSet, TPrincipal>
        where TChangeSet : IChangeSet<TPrincipal>
    {
        private TChangeSet set;
        private IChangeSetFactory<TChangeSet, TPrincipal> factory;
        private DeferredValueMap<IObjectChange<TPrincipal>> deferredValues;

        public Recorder(IChangeSetFactory<TChangeSet, TPrincipal> factory)
        {
            this.deferredValues = new DeferredValueMap<IObjectChange<TPrincipal>>();
            this.factory = factory;
        }

        public bool HasChangeSet { get { return set != null; } }

        public void Record(object entity, string reference, string propertyName, Func<object> deferredValue, EntityState entityState, Func<object> oldValue)
        {
            ensureChangeSetExists();

            string typeName = ObjectContext.GetObjectType(entity.GetType()).Name;
            string key = reference;
            var objectChange = getObjectChangeFor(set, typeName, key, entityState);

            record(objectChange, propertyName, deferredValue, oldValue);
        }
        private void record(IObjectChange<TPrincipal> objectChange, string propertyName, Func<object> deferredValue, Func<object> oldValue)
        {
            IPropertyChange<TPrincipal> propertyChange = getPropertyChangeFor(objectChange, propertyName);
            if (deferredValue != null)
            {
                deferredValues.Store(objectChange, propertyName, deferredValue, oldValue);
            }
        }

        /// <summary>
        /// The work required to calculate the values for some changes (many-to-many)
        /// is not side-effect free, so we can't do it while the changes from the db
        /// commit are hanging around.
        /// Instead, we record deferred values - pieces of calculation to do once the
        /// database changes are safely saved out. Then, before committing the change
        /// set, we "bake" in the values - we actually do the deferred calculation and
        /// store it in the PropertyChange.
        /// </summary>
        public TChangeSet Bake(DateTime timestamp, TPrincipal author)
        {
            set.Author = author;
            set.Timestamp = timestamp;

            foreach (var objectChange in set.ObjectChanges)
            {
                if (deferredValues.HasContainer(objectChange))
                    bake(objectChange);
            }
            return set;
        }
        private void bake(IObjectChange<TPrincipal> objectChange)
        {
            var bakedValues = deferredValues.CalculateAndRetrieve(objectChange);
            foreach (KeyValuePair<string, Tuple<object, object>> kv in bakedValues)
            {
                var propertyChange = getPropertyChangeFor(objectChange, kv.Key);
                setValue(propertyChange, kv.Value.Item1, kv.Value.Item2);
            }
        }
        private void setValue(IPropertyChange<TPrincipal> propertyChange, object value, object oldValue)
        {            
            string valueAsString = valueToString(value);
            string oldValueAsString = valueToString(oldValue);
            int valueAsInt;

            propertyChange.Value = valueAsString;
            propertyChange.OldValue = oldValueAsString;
            
            if (int.TryParse(propertyChange.Value, out valueAsInt))
            {
                propertyChange.ValueAsInt = valueAsInt;
            }
        }
        private string valueToString(object value)
        {
            if (value == null)
                return null;
            else
                return value.ToString();
        }

        private IObjectChange<TPrincipal> get(TChangeSet set, string typeName, object reference)
        {
            return set.ObjectChanges.Single(s => s.TypeName == typeName && s.ObjectReference == reference.ToString());
        }
        private IObjectChange<TPrincipal> getObjectChangeFor(TChangeSet set, string typeName, string key, EntityState entityState)
        {
            var result = set.ObjectChanges.SingleOrDefault(oc => oc.TypeName == typeName
                && oc.ObjectReference == key);
            if (result == null)
            {
                result = factory.ObjectChange();
                result.TypeName = typeName;
                result.ObjectReference = key;
                result.ChangeSet = set;
                result.OperationType = entityState;
                set.Add(result);
            }
            return result;
        }
        private IPropertyChange<TPrincipal> getPropertyChangeFor(IObjectChange<TPrincipal> objectChange, string propertyName)
        {
            var result = objectChange.PropertyChanges.SingleOrDefault(pc => pc.PropertyName == propertyName);
            if (result == null)
            {
                result = factory.PropertyChange();
                result.ObjectChange = objectChange;
                result.PropertyName = propertyName;
                result.Value = null;
                result.ValueAsInt = null;
                objectChange.Add(result);
            }
            return result;
        }
        private void ensureChangeSetExists()
        {
            if (set == null)
                set = factory.ChangeSet();
        }
    }
}