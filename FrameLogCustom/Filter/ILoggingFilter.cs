using System;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;

namespace FrameLog.Filter
{
    /// <summary>
    /// A logging filter determines whether a given class should be logged,
    /// and then, if it should be, whether any properties within it should be
    /// logged.
    /// 
    /// If a class should be logged, but none of its properties should be,
    /// nothing is logged.
    /// </summary>
    public interface ILoggingFilter
    {        
        /// <summary>
        /// True if the given class should be logged
        /// </summary>
        bool ShouldLog(Type type);        
        /// <summary>
        /// True if the given navigation property should be logged
        /// </summary>
        bool ShouldLog(NavigationProperty property);
        /// <summary>
        /// True if the given scalar property should be logged (as identified
        /// by the object type it belongs to, and its name as a string)
        /// </summary>
        bool ShouldLog(Type type, string propertyName);
    }
}
