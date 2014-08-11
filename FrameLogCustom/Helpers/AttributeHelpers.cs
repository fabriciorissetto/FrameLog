using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace FrameLog.Helpers
{
    public static class AttributeHelpers
    {
        /// <summary>
        /// Returns a single attribute of type T, if the member has it.
        /// Otherwise returns null.
        /// </summary>
        public static T GetAttribute<T>(this MemberInfo member)
        {
            return member.GetCustomAttributes(typeof(T), true).Cast<T>().SingleOrDefault();
        }

        /// <summary>
        /// Returns all attributes of type T on the property, with reference to
        /// metadata types of the declaring type, as defined by any MetadataTypeAttribute.
        /// </summary>
        public static IEnumerable<T> GetAttributes<T>(this PropertyInfo property)
        {
            return property.GetAttributes<T>(GetMetadataTypes(property.DeclaringType));
        }
        /// <summary>
        /// Returns all attributes of type T on the class, with reference to
        /// metadata types as defined by any MetadataTypeAttribute.
        /// </summary>
        public static IEnumerable<T> GetAttributes<T>(this Type type)
        {
            return GetMetadataTypes(type).GetAttributes<T>();
        }

        private static IEnumerable<T> GetAttributes<T>(this PropertyInfo property, IEnumerable<Type> types)
        {
            List<T> attributes = new List<T>();
            foreach (Type type in types)
            {
                PropertyInfo metaProperty = type.GetProperty(property.Name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (metaProperty != null)
                    attributes.AddRange(metaProperty.GetCustomAttributes(false).Where(a => a is T).Cast<T>());
            }
            return attributes;
        }
        private static IEnumerable<T> GetAttributes<T>(this IEnumerable<Type> types)
        {
            List<T> attributes = new List<T>();
            foreach (Type type in types)
            {
                 attributes.AddRange(type.GetCustomAttributes(false).Where(a => a is T).Cast<T>());
            }
            return attributes;
        }

        private static IEnumerable<Type> GetMetadataTypes(Type type)
        {
            yield return type;

            var meta = type.GetAttribute<MetadataTypeAttribute>();
            if (meta != null)
                yield return meta.MetadataClassType;
        }
    }
}
