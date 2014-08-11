using FrameLog.Contexts;
using System.Collections.Generic;
using System.Linq;

namespace FrameLog.Filter
{
    /// <summary>
    /// Logs everything that is not explicitly blacklisted.
    /// A property or type is not logged only if it has at least one IFilterAttribute 
    /// that returns "false".
    /// </summary>
    public class BlacklistLoggingFilter : AttributeBasedLoggingFilter, ILoggingFilter
    {
        public BlacklistLoggingFilter(IFrameLogContext context)
            : base(context) { }

        protected override bool shouldLogFromAttributes(IEnumerable<IFilterAttribute> filters)
        {
            return filters.All(f => f.ShouldLog());
        }

        public class Provider : ILoggingFilterProvider
        {
            public ILoggingFilter Get(IFrameLogContext context)
            {
                return new BlacklistLoggingFilter(context);
            }
        }
    }
}
