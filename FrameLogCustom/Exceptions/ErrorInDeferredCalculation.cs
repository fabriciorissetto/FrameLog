using System;

namespace FrameLog.Exceptions
{
    [Serializable]
    public class ErrorInDeferredCalculation : Exception
    {
        public ErrorInDeferredCalculation(object container, string key, Exception innerException)
            : base(messageFor(container, key, innerException), innerException) { }

        private static string messageFor(object container, string key, Exception innerException)
        {
            return string.Format("An error of type '{2}' occured during deferred calculation of property '{0}' on container '{1}'. See inner exception for more details.",
                key, container, innerException.GetType());
        }
    }
}
