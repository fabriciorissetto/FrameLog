using System;
using System.Data.Entity.Core;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Reflection;
using FrameLog.Models;

namespace FrameLog.Contexts
{
    public abstract class ObjectContextAdapter<TChangeSet, TPrincipal> 
        : IFrameLogContext<TChangeSet, TPrincipal>
        where TChangeSet : IChangeSet<TPrincipal>
    {
        private ObjectContext context;

        public ObjectContextAdapter(ObjectContext context)
        {
            this.context = context;
        }

        public int SaveChanges(SaveOptions options)
        {
            return context.SaveChanges(options);
        }

        public object GetObjectByKey(EntityKey key)
        {
            return context.GetObjectByKey(key);
        }

        public virtual string KeyPropertyName
        {
            get { return "Id"; }
        }
        public virtual object KeyFromReference(string reference)
        {
            return int.Parse(reference);
        }
        public virtual object GetObjectByReference(Type type, string reference)
        {
            var container = context.MetadataWorkspace.GetEntityContainer(context.DefaultContainerName, DataSpace.CSpace);
            var set = container.BaseEntitySets.First(meta => meta.ElementType.Name == type.Name);
            var key = new EntityKey(container.Name + "." + set.Name, KeyPropertyName, KeyFromReference(reference));
            return GetObjectByKey(key);
        }
        public virtual string GetReferenceForObject(object entity)
        {
            if (entity == null)
                return null;

            IHasLoggingReference entityWithReference = entity as IHasLoggingReference;
            if (entityWithReference != null)
                return entityWithReference.Reference.ToString();

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.IgnoreCase;
            string keyPropertyName = GetReferencePropertyForObject(entity);
            var keyProperty = entity.GetType().GetProperty(keyPropertyName, flags);
            if (keyProperty != null)
                return keyProperty.GetGetMethod().Invoke(entity, null).ToString();

            throw new Exception(string.Format("Attempted to log a foreign entity that did not implement IHasLoggingReference and that did not have a property with name '{0}'. It had type {1}, and it was '{2}'",
                    KeyPropertyName, entity.GetType(), entity));
        }
        public virtual string GetReferencePropertyForObject(object entity)
        {
            return KeyPropertyName;
        }

        public ObjectStateManager ObjectStateManager
        {
            get { return context.ObjectStateManager; }
        }
        public MetadataWorkspace Workspace
        {
            get { return context.MetadataWorkspace; }
        }
        public abstract Type UnderlyingContextType { get; }

        public void AcceptAllChanges()
        {
            context.AcceptAllChanges();
        }

        public abstract IQueryable<IChangeSet<TPrincipal>> ChangeSets { get; }
        public abstract IQueryable<IObjectChange<TPrincipal>> ObjectChanges { get; }
        public abstract IQueryable<IPropertyChange<TPrincipal>> PropertyChanges { get; }
        public abstract void AddChangeSet(TChangeSet changeSet);
    }
}
