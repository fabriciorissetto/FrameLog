using System;

namespace FrameLog.History
{
    public class Change<TValue, TPrincipal> : IChange<TValue, TPrincipal>
    {
        public Change(TValue value, TPrincipal author, DateTime timestamp)
        {
            Value = value;
            Author = author;
            Timestamp = timestamp;
        }

        public DateTime Timestamp { get; private set; }
        public TPrincipal Author { get; private set; }
        public TValue Value { get; private set; }

        public override bool Equals(object obj)
        {
            var other = obj as Change<TValue, TPrincipal>;
            if (other == null)
                return false;

            return object.Equals(Author, other.Author)
                && object.Equals(Timestamp, other.Timestamp)
                && object.Equals(Value, other.Value);                
        }
        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
        public override string ToString()
        {
            return string.Format("{0}:{1}:{2}", Author, Timestamp, Value);
        }
    }
}