using System;
using System.Runtime.Serialization;

namespace HexMage.Simulator.Model {
    public class InvariantViolationException : Exception {
        public InvariantViolationException() { }
        public InvariantViolationException(string message) : base(message) { }
        public InvariantViolationException(string message, Exception innerException) : base(message, innerException) { }
        protected InvariantViolationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}