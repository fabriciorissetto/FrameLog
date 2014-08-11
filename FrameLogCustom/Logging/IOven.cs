using FrameLog.Models;
using System;

namespace FrameLog.Logging
{
    internal interface IOven<TChangeSet, TPrincipal>
        where TChangeSet : IChangeSet<TPrincipal>
    {
        bool HasChangeSet { get; }
        TChangeSet Bake(DateTime timestamp, TPrincipal author);
    }
}
