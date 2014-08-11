
namespace FrameLog.Filter
{
    public static class Filters
    {    
        /// <summary>
        /// By default, logs everything. A property or type is not logged only if it
        /// has at least one IFilterAttribute that returns "false".
        /// </summary>
        public static readonly ILoggingFilterProvider Greedy = new FrameLog.Filter.BlacklistLoggingFilter.Provider();    
        /// <summary>
        /// By default, logs nothing. A property or type is logged only if it
        /// has at least one IFilterAttribute, and all IFilterAttributes 
        /// return "true". 
        /// 
        /// Note that to log a given property you first need to mark
        /// the enclosing class with DoLog, and then the also the property.
        /// </summary>
        public static readonly ILoggingFilterProvider Sparse = new FrameLog.Filter.WhitelistLoggingFilter.Provider();

        /// <summary>
        /// By default, logs everything. A property or type is not logged only if it
        /// has at least one IFilterAttribute that returns "false".
        /// </summary>
        public static ILoggingFilterProvider Default { get { return Greedy; } }
    }
}
