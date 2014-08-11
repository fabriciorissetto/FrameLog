using FrameLog.Contexts;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;

namespace FrameLog.Filter
{
    /// <summary>
    /// This class provides a basis for writing logging filters based on the 
    /// IFilterAttributes.
    /// 
    /// Because retrieving the attributes involves mapping from Entity Framework
    /// metadata to objects in memory, and then using reflection it can be quite
    /// a slow process. Therefore, this class caches which attributes are applicable
    /// to each class and property using a global instance of FilterAttributeCache.
    /// See FilterAttributeCache.For(IFrameLogContext) for more details.
    /// </summary>
    public abstract class AttributeBasedLoggingFilter : ILoggingFilter
    {
        private FilterAttributeCache cache;
        
        public AttributeBasedLoggingFilter(IFrameLogContext context)
        {
            cache = FilterAttributeCache.For(context);
        }

        public bool ShouldLog(Type type)
        {
            return shouldLogFromAttributes(cache.AttributesFor(type));
        }
        public bool ShouldLog(NavigationProperty property)
        {
            return shouldLogFromAttributes(cache.AttributesFor(property));
        }
        public bool ShouldLog(Type type, string propertyName)
        {
            return shouldLogFromAttributes(cache.AttributesFor(type, propertyName));
        }

        protected abstract bool shouldLogFromAttributes(IEnumerable<IFilterAttribute> filters);
    }


}
