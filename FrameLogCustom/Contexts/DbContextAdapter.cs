using System.Data.Entity;
using FrameLog.Models;

namespace FrameLog.Contexts
{
    public abstract class DbContextAdapter<TChangeSet, TPrincipal> 
        : ObjectContextAdapter<TChangeSet, TPrincipal>
        where TChangeSet : IChangeSet<TPrincipal>
    {
        public DbContextAdapter(DbContext context)
            : base(((System.Data.Entity.Infrastructure.IObjectContextAdapter)context).ObjectContext) { }
    }
}
