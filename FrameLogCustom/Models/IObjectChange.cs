using System.Collections.Generic;
using System.Data.Entity;

namespace FrameLog.Models
{
    public interface IObjectChange<TPrincipal>
    {
        IChangeSet<TPrincipal> ChangeSet { get; set; }
        IEnumerable<IPropertyChange<TPrincipal>> PropertyChanges { get; }
        void Add(IPropertyChange<TPrincipal> propertyChange);

        string TypeName { get; set; }
        string ObjectReference { get; set; }
        EntityState OperationType { get; set; }
    }
}
