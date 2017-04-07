using System;

namespace HexMage.Simulator.Model {
    public class TemporarilySuspendReplayRecording : IDisposable {
        public TemporarilySuspendReplayRecording() {
            Constants.RecordReplays = false;
        }

        public void Dispose() {
            Constants.RecordReplays = true;
        }
    }
}