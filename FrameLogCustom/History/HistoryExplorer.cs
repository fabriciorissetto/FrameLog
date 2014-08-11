using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using FrameLog.Contexts;
using FrameLog.Helpers;
using FrameLog.Models;

namespace FrameLog.History
{
    /// <summary>
    /// This class reconstitutes logs into sequences of values and objects, accompanied
    /// by timestamp and author data.
    /// </summary>
    public class HistoryExplorer<TChangeSet, TPrincipal>
        where TChangeSet : IChangeSet<TPrincipal>
    {
        private IHistoryContext<TChangeSet, TPrincipal> db;

        public HistoryExplorer(IHistoryContext<TChangeSet, TPrincipal> db)
        {
            this.db = db;
        }

        /// <summary>
        /// Retrieve the values that a single property has gone through, most recent
        /// first (descending date order).
        /// </summary>
        public virtual IEnumerable<IChange<TValue, TPrincipal>> ChangesTo<TModel, TValue>(TModel model, Expression<Func<TModel, TValue>> property)
        {
            string typeName = typeof(TModel).Name;
            string propertyName = property.GetPropertyName();
            string reference = db.GetReferenceForObject(model);

            return db.ObjectChanges
                .Where(o => o.TypeName == typeName)
                .Where(o => o.ObjectReference == reference)
                .SelectMany(o => o.PropertyChanges)
                .Where(p => p.PropertyName == propertyName)
                .OrderByDescending(p => p.ObjectChange.ChangeSet.Timestamp)
                .AsEnumerable()
                .Select(p => new Change<TValue, TPrincipal>(bind<TValue>(p.Value), p.ObjectChange.ChangeSet.Author, p.ObjectChange.ChangeSet.Timestamp));
        }
        /// <summary>
        /// Rehydrates versions of the object, one for each logged change to the object,
        /// most recent first (descending date order).
        /// </summary>
        public virtual IEnumerable<IChange<TModel, TPrincipal>> ChangesTo<TModel>(TModel model)
            where TModel : ICloneable, new()
        {
            var changes = changesTo(model);
            return applyChangesTo(new TModel(), changes)
                .OrderByDescending(c => c.Timestamp);
        }
        /// <summary>
        /// Returns the timestamp and author information for the creation of the object.
        /// If the creation of the object is not recorded in the log, throws a
        /// CreationDoesNotExistInLogException.
        /// </summary>
        public virtual IChange<TModel, TPrincipal> GetCreation<TModel>(TModel model)
        {
            var firstChange = changesTo(model).FirstOrDefault();
            if (firstChange == null || !isCreation(model, firstChange))
                throw new CreationDoesNotExistInLogException(model);
            else
            {
                var set = firstChange.ChangeSet;
                return new Change<TModel, TPrincipal>(model, set.Author, set.Timestamp);
            }
        }

        /// <summary>
        /// Returns all IObjectChanges that are relevant to this object, earliest first
        /// </summary>
        protected virtual IOrderedQueryable<IObjectChange<TPrincipal>> changesTo<TModel>(TModel model)
        {
            string typeName = typeof(TModel).Name;
            string reference = db.GetReferenceForObject(model);

            var changes = db.ObjectChanges
                .Where(o => o.TypeName == typeName)
                .Where(o => o.ObjectReference == reference)
                .OrderBy(o => o.ChangeSet.Timestamp);
            return changes;
        }
        /// <summary>
        /// Given a starting state of the object ("seed") and an ordered series of changes, returns an
        /// ordered series of objects of type TModel. The first object returned is the seed state with
        /// the first change applied to it. Each subsequent object returned is the previous object with 
        /// the next change applied.
        /// 
        /// The seed is simply a blank object to use as a strongly-typed starting point. It is expected
        /// that the first change applied will be the object's creation, assuming logs are complete,
        /// and that this will set every field.
        /// </summary>
        protected virtual IEnumerable<IChange<TModel, TPrincipal>> applyChangesTo<TModel>(TModel seed, IEnumerable<IObjectChange<TPrincipal>> changes)
            where TModel : ICloneable
        {
            TModel current = seed;
            foreach (var change in changes)
            {
                var c = apply(change, current);
                yield return c;
                current = c.Value;
            }
        }

        protected virtual IChange<TModel, TPrincipal> apply<TModel>(IObjectChange<TPrincipal> change, TModel model)
            where TModel : ICloneable
        {
            var type = typeof(TModel);
            var newVersion = (TModel)model.Clone();
            foreach (var propertyChange in change.PropertyChanges)
            {
                var property = model.GetType().GetProperty(propertyChange.PropertyName, 
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                var value = bind(propertyChange.Value, property.PropertyType);
                property.SetValue(newVersion, value, null);
            }
            return new Change<TModel, TPrincipal>(newVersion, 
                change.ChangeSet.Author, 
                change.ChangeSet.Timestamp);
        }

        /// <summary>
        /// Given an object change for a particular object, returns true if this object
        /// change represents the object being added to the database, rather than a subsequent
        /// update or delete.
        /// </summary>
        protected virtual bool isCreation(object model, IObjectChange<TPrincipal> change)
        {
            string primaryKeyField = db.GetReferencePropertyForObject(model);
            return change.PropertyChanges
                .Select(p => p.PropertyName)
                .Contains(primaryKeyField);
        }

        protected virtual TValue bind<TValue>(string raw)
        {
            return (TValue)bind(raw, typeof(TValue));
        }
        protected virtual object bind(string raw, Type type)
        {
            if (type.IsPrimitive || type == typeof(string))
                return Convert.ChangeType(raw, type);
            else if (typeof(DateTime?).IsAssignableFrom(type))
                return bindDateTime(raw);
            else if (isCollectionType(type))
                return bindCollection(raw, type);
            else
            {
                if (raw == null)
                    return null;
                else
                    return db.GetObjectByReference(type, raw);
            }
        }
        protected virtual bool isCollectionType(Type type)
        {
            return type.IsGenericType
                && typeof(ICollection<>).MakeGenericType(type.GetGenericArguments().First()).IsAssignableFrom(type);
        }
        protected virtual object bindCollection(string raw, Type type)
        {
            var itemType = type.GetGenericArguments().First();
            object collection = Activator.CreateInstance(type);
            GetType().GetMethod("fillCollection", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)
                .MakeGenericMethod(new Type[] { itemType })
                .Invoke(this, new object[] { collection, raw });
            return collection;
        }
        protected virtual void fillCollection<ItemType>(ICollection<ItemType> collection, string raw)
        {
            foreach (var reference in raw.Split(new char[] { ',' }))
            {
                collection.Add(bind<ItemType>(reference));
            }
        }

        protected static DateTime? bindDateTime(string raw)
        {
            try
            {
                return DateTime.Parse(raw);
            }
            catch (ArgumentNullException)
            {
                return null;
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}