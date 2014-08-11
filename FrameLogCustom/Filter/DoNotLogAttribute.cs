using System;

namespace FrameLog.Filter
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
    public class DoNotLogAttribute : Attribute, IFilterAttribute
    {
        public bool ShouldLog()
        {
            return false;
        }
    }
}
