
namespace FrameLog.Models
{
    public interface IPropertyChange<TPrincipal>
    {
        IObjectChange<TPrincipal> ObjectChange { get; set; }
        string PropertyName { get; set; }
        string Value { get; set; }
        int? ValueAsInt { get; set; }
        string OldValue { get; set; }
    }
}
