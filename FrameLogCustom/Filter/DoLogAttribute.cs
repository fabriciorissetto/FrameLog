using System;

namespace FrameLog.Filter
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
    public class DoLogAttribute : Attribute, IFilterAttribute
    {
        public bool ShouldLog()
        {
            return true;
        }
    }
}
