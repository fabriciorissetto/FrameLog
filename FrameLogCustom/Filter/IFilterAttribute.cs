using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FrameLog.Filter
{
    public interface IFilterAttribute
    {
        bool ShouldLog();
    }
}
