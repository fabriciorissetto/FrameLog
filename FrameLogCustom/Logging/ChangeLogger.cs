using FrameLog.Contexts;
using FrameLog.Filter;
using FrameLog.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Reflection;

namespace FrameLog.Logging
{
    internal class ChangeLogger<TChangeSet, TPrincipal>
        where TChangeSet : IChangeSet<TPrincipal>
    {
        private IFrameLogContext<TChangeSet, TPrincipal> context;
        private IChangeSetFactory<TChangeSet, TPrincipal> factory;
        private Recorder<TChangeSet, TPrincipal> recorder;
        private ILoggingFilter filter;

        public ChangeLogger(IFrameLogContext<TChangeSet, TPrincipal> context, 
            IChangeSetFactory<TChangeSet, TPrincipal> factory,
            ILoggingFilter filter)
        {
            this.context = context;
            this.factory = factory;
            this.recorder = new Recorder<TChangeSet,TPrincipal>(factory);
            this.filter = filter;
        }

        public IOven<TChangeSet, TPrincipal> Log(ObjectStateManager objectStateManager)
        {
            var entries = objectStateManager.GetObjectStateEntries(EntityState.Added
                | EntityState.Modified
                | EntityState.Deleted);

            foreach (var entry in entries)
                process(entry);

            return recorder;
        }

        private void process(ObjectStateEntry entry)
        {
            // There are two kinds of changes that the ObjectStateManager gives us.
            // See the two methods for more information.
            
            if (entry.IsRelationship)
            {
                logRelationshipChange(entry);
            }
            else
            {
                logScalarChanges(entry);
            }
        }

        private void logScalarChanges(ObjectStateEntry entry)
        {
            // If this class shouldn't be logged, give up at this point
            if (!filter.ShouldLog(entry.Entity.GetType()))
                return;

            // Scalar changes are generated when a non-navigation property
            // (like a string or an int) is changed. EF gives these to us in this format:
            // - The entity which owns the property
            // - The property
            // - The old and new values.
            // In other words, these are already in the right format for our logs.
            foreach (string propertyName in getChangedProperties(entry))
            {
                if (filter.ShouldLog(entry.Entity.GetType(), propertyName))
                {
                    var valuePair = getValuePair(entry, propertyName);
                    if (valuePair.HasChanged)
                    {
                        recorder.Record(entry.Entity,
                            context.GetReferenceForObject(entry.Entity),
                            propertyName,
                            () => valuePair.NewValue,
                            entry.State,
                            () => valuePair.OriginalValue);
                    }
                }
            }
        }

        private void logRelationshipChange(ObjectStateEntry entry)
        {           
            // Relationship changes are generated when a navigation property
            // is updated. EF views this as the creation (and deletion, potentially)
            // of a relationship object. So if Book.Sequel is updated from "Dracula" to "Dune"
            // then we see that here as the creation of an FK_Book_Sequel object that 
            // links a particular Book with Dune, and at the same time as the deletion
            // of an object that links that Book with the Dracula object. 
            // This means we have to do some work to transform to our target format.

            if (entry.State == EntityState.Added || entry.State == EntityState.Deleted)
            {
                // Each relationship has two ends. We log in both directions.
                var ends = getAssociationEnds(entry);
                foreach (var localEnd in ends)
                {
                    var foreignEnd = getOtherAssociationEnd(entry, localEnd);
                    logForeignKeyChange(entry, localEnd, foreignEnd);
                }
            }
        }

        private void logForeignKeyChange(ObjectStateEntry entry, AssociationEndMember localEnd, AssociationEndMember foreignEnd)
        {
            // These "keys" represent in-memory unique references
            // to the objects at the ends of these associations.
            // In the case of new objects, these won't contain the primary key that has just
            // been generated, but we can still use it below to get to the real object in-memory
            // and pull the id out of that.
            var key = getEndEntityKey(entry, localEnd);

            // Get the object identified by the local key
            object entity = context.GetObjectByKey(key);
            if (!filter.ShouldLog(entity.GetType()))
                return;

            // The property on the "local" object that navigates to the "foreign" object
            var property = getProperty(entry, localEnd, foreignEnd, key);
            // We can control which directions of relationships we are interested in logging
            // by which navigation properties we keep in the model
            if (property == null || !filter.ShouldLog(property))
                return;

            // Generate the change
            Func<object> value = getForeignValue(entry, entity, foreignEnd, property.Name);

            recorder.Record(entity, context.GetReferenceForObject(entity), property.Name, value, entry.State, null /* TODO: Fabricio retirar */);
        }

        private Func<object> getForeignValue(ObjectStateEntry entry, object entity, AssociationEndMember foreignEnd, string propertyName)
        {
            // Get the key that identifies the the object we are making or breaking a relationship with
            var foreignKey = getEndEntityKey(entry, foreignEnd);
            string change = getKeyReference(foreignKey);

            if (foreignEnd.RelationshipMultiplicity == RelationshipMultiplicity.Many)
            {
                return manyToManyValue(entity, propertyName);
            }
            else
            {
                if (entry.State == EntityState.Added)
                    return () => change;
                else
                    return null;
            }
        }

        private Func<object> manyToManyValue(object entity, string propertyName)
        {
            return () =>
            {
                // In this case the key id represents an object being added to or removed from a set.
                // We use reflection to get the current contents of the set (so after the change we are logging).
                if (entity == null)
                    throw new InvalidOperationException("Attempted to log change to null object of type " + entity.GetType().Name);

                var property = entity.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (property == null)
                    throw new InvalidOperationException(string.Format("Unable to find a property with name '{0}' on type '{1}'", propertyName, entity.GetType()));

                var value = property.GetValue(entity, null);
                if (value == null)
                    throw new InvalidOperationException(string.Format("Many-to-many set '{0}.{1}' was null", entity.GetType().Name, property.Name));
                IEnumerable<string> current = ((IEnumerable<object>)value)
                    .Select(e => context.GetReferenceForObject(e))
                    .Distinct()
                    .OrderBy(reference => reference);
                return toIdList(current);
            };
        }
        private string toIdList(IEnumerable<string> references)
        {
            return string.Format("{0}", string.Join(",", references));
        }

        private string getKeyReference(EntityKey key)
        {
            var entity = context.GetObjectByKey(key);
            return context.GetReferenceForObject(entity);
        }

        private ValuePair getValuePair(ObjectStateEntry entry, string property)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    return new ValuePair(null, entry.CurrentValues[property], entry.State);
                case EntityState.Deleted:
                    return new ValuePair(entry.OriginalValues[property], null, entry.State);
                case EntityState.Modified:
                    return new ValuePair(entry.OriginalValues[property], 
                        entry.CurrentValues[property], entry.State);
                default:
                    throw new NotImplementedException(string.Format("Unable to deal with ObjectStateEntry in '{0}' state",
                        entry.State));
            }
        }

        private IEnumerable<string> getChangedProperties(ObjectStateEntry entry)
        {
            var values = useableValues(entry);
            for (int i = 0; i < values.FieldCount; i++)
                yield return values.GetName(i);
        }

        /// <summary>
        /// Gets either CurrentValues or OriginalValues depending on which the entry
        /// makes available. (e.g. entries that are Added have no OriginalValues, and
        /// entries that are Deleted only have OriginalValues).
        /// </summary>
        private IExtendedDataRecord useableValues(ObjectStateEntry entry)
        {
            return entry.State == EntityState.Deleted
                ? (IExtendedDataRecord)entry.OriginalValues
                : entry.CurrentValues;
        }

        private AssociationEndMember[] getAssociationEnds(ObjectStateEntry entry)
        {
            var fieldMetadata = useableValues(entry).DataRecordInfo.FieldMetadata;
            return fieldMetadata
                .Select(m => m.FieldType as AssociationEndMember)
                .ToArray();
        }

        /// <summary>
        /// Given one end of an association, fetches the other end
        /// </summary>
        private AssociationEndMember getOtherAssociationEnd(ObjectStateEntry entry, AssociationEndMember end)
        {
            AssociationEndMember[] ends = getAssociationEnds(entry);
            if (ends[0] == end)
                return ends[1];
            else
                return ends[0];
        }

        /// <summary>
        /// Gets the EntityKey associated with this end of the association
        /// </summary>
        private EntityKey getEndEntityKey(ObjectStateEntry entry, AssociationEndMember end)
        {
            AssociationEndMember[] ends = getAssociationEnds(entry);
            if (ends[0] == end)
                return useableValues(entry)[0] as EntityKey;
            else
                return useableValues(entry)[1] as EntityKey;
        }

        /// <summary>
        /// Gets the NavigationProperty that allows you to navigate from entity represented
        /// by the localEnd to the entity represented by the foreignEnd.
        /// </summary>
        private NavigationProperty getProperty(ObjectStateEntry entry, 
            AssociationEndMember localEnd, AssociationEndMember foreignEnd, 
            EntityKey key)
        {
            var relationshipType = entry.EntitySet.ElementType;
            var entitySet = key.GetEntitySet(entry.ObjectStateManager.MetadataWorkspace);
            return entitySet.ElementType.NavigationProperties
                .Where(p => p.RelationshipType == relationshipType
                         && p.FromEndMember == localEnd
                         && p.ToEndMember == foreignEnd)
                .SingleOrDefault();
        }
    }
}