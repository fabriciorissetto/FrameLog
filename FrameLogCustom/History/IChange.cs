using System;

namespace FrameLog.History
{
    public interface IChange<TValue, TPrincipal>
    {
        DateTime Timestamp { get; }
        TPrincipal Author { get; }
        TValue Value { get; }
    }
}
