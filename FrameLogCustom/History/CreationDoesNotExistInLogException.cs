using System;
using System.Runtime.Serialization;

namespace FrameLog.History
{
    [Serializable]
    public class CreationDoesNotExistInLogException : Exception
    {
        public readonly object Model;

        public CreationDoesNotExistInLogException(object model)
            : base(string.Format("There is no record of this object's creation in the log. Object: '{0}'.", model))
        {
            Model = model;
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }
}
