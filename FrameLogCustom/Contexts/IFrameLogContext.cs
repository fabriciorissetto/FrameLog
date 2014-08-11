using FrameLog.Models;
using System;
using System.Data.Entity.Core;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;

namespace FrameLog.Contexts
{
    public interface IFrameLogContext
    {
        Type UnderlyingContextType { get; }
        MetadataWorkspace Workspace { get; }
    }

    public interface IFrameLogContext<TChangeSet, TPrincipal> : IHistoryContext<TChangeSet, TPrincipal>, IFrameLogContext
        where TChangeSet : IChangeSet<TPrincipal>
    {
        int SaveChanges(SaveOptions options);
        ObjectStateManager ObjectStateManager { get; }
        void AcceptAllChanges();

        object GetObjectByKey(EntityKey key);
        void AddChangeSet(TChangeSet changeSet);
    }
}
