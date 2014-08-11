using System;
using System.Collections.Generic;

namespace FrameLog.Models
{
    public interface IChangeSet<TPrincipal>
    {
        IEnumerable<IObjectChange<TPrincipal>> ObjectChanges { get; }
        void Add(IObjectChange<TPrincipal> objectChange);

        DateTime Timestamp { get; set; }
        TPrincipal Author { get; set; }
    }
}
