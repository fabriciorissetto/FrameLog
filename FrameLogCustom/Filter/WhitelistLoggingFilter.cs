using FrameLog.Contexts;
using System.Collections.Generic;
using System.Linq;

namespace FrameLog.Filter
{    
    /// <summary>
    /// Logs only the things that are explicitly whitelisted.
    /// By default, logs nothing. A property or type is logged only if it
    /// has at least one IFilterAttribute, and all IFilterAttributes 
    /// return "true". 
    /// 
    /// Note that to log a given property you first need to mark
    /// the enclosing class with DoLog, and then the also the property.
    /// </summary>
    public class WhitelistLoggingFilter : AttributeBasedLoggingFilter, ILoggingFilter
    {
        public WhitelistLoggingFilter(IFrameLogContext context)
            : base(context) { }

        protected override bool shouldLogFromAttributes(IEnumerable<IFilterAttribute> filters)
        {
            return filters.Any() && filters.All(f => f.ShouldLog());
        }

        public class Provider : ILoggingFilterProvider
        {
            public ILoggingFilter Get(IFrameLogContext context)
            {
                return new WhitelistLoggingFilter(context);
            }
        }
    }
}
