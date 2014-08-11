using FrameLog.Filter;
using System;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace FrameLog.Helpers
{
    /// <summary>
    /// Provides mapping services between reflection info and EDM conceptual types.
    /// This class constructs a TypeLookup, which is an expensive object, so you should
    /// use a longlived instance of this object.
    /// </summary>
    public class MetadataSpaceMapper
    {
        private readonly MetadataWorkspace workspace;
        private readonly TypeLookup typeLookup;
        private static readonly BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public MetadataSpaceMapper(MetadataWorkspace workspace)
        {
            this.workspace = workspace;
            this.typeLookup = new TypeLookup();
        }

        public PropertyInfo Map(NavigationProperty navigationProperty)
        {
            var objectSpaceType = workspace.GetObjectSpaceType(navigationProperty.DeclaringType);
            Type type = typeLookup.Map(objectSpaceType);
            return Map(type, navigationProperty.Name);
        }
        public NavigationProperty Map(PropertyInfo property)
        {
            var objectSpaceType = workspace.GetItems<StructuralType>(DataSpace.OSpace).Single(i => i.Name == property.DeclaringType.Name);
            var cspaceType = workspace.GetEdmSpaceType(objectSpaceType);
            return (NavigationProperty)cspaceType.Members.Single(m => m.Name == property.Name);
        }

        public NavigationProperty Map<TModel, TValue>(TModel model, Expression<Func<TModel, TValue>> propertyExpression)
        {
            return Map(typeof(TModel).GetProperty(propertyExpression.GetPropertyName(), flags));
        }

        public PropertyInfo Map(Type type, string propertyName)
        {
            return type.GetProperty(propertyName, flags);
        }
    }
}
