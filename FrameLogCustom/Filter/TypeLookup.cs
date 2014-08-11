using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Linq;
using System.Reflection;

namespace FrameLog.Filter
{
    /// <summary>
    /// We need to be able to translate from the namespace-qualified but *not*
    /// asssembly-qualified names that the MetadataWorkspace gives us. To this
    /// end, we build an internal lookup of all the types in all assemblies at
    /// the point when the TypeLookup is constructed. This is typically when
    /// the database context is first created.
    /// </summary>
    internal class TypeLookup
    {
        private ConcurrentDictionary<string, Type> lookup;
        private List<ReflectionTypeLoadException> typeLoadErrors;

        internal TypeLookup()
        {
            // We store errors and don't throw them. If and when a type is requested
            // that we don't have for some reason we will dust these errors off and
            // attach them to the exception that gets thrown.
            this.typeLoadErrors = new List<ReflectionTypeLoadException>();
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(getTypes)
                .Distinct();
            lookup = new ConcurrentDictionary<string, Type>();
            foreach (var type in types)
            {
                // If there are two types with the same namespace-qualified name,
                // only one of them will end up in the lookup. This is potentially
                // a problem.
                lookup[type.FullName] = type;
            }
        }

        internal Type Map(StructuralType objectSpaceType)
        {
            Type type;
            if (!lookup.TryGetValue(objectSpaceType.FullName, out type))
            {
                // For some reason the type wasn't in the typeLookup we built when we
                // constructed this class. If we encountered any errors building that lookup
                // they will be in the typeLoadErrors collection we pass to the exception.
                throw new UnknownTypeException(objectSpaceType.FullName, typeLoadErrors);
            }
            return type;
        }

        /// <summary>
        /// Gets the types and stores the errors for later. See the comment in 
        /// buildTypeLookup()
        /// </summary>
        private IEnumerable<Type> getTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                typeLoadErrors.Add(e);
                return new List<Type>();
            }
        }
    }
}
