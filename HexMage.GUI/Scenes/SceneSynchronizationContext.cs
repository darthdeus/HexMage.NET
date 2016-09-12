using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using HexMage.Simulator;

namespace HexMage.GUI {
    public class SceneSynchronizationContext : SynchronizationContext {
        ConcurrentQueue<KeyValuePair<SendOrPostCallback, object>> _queue = new ConcurrentQueue<KeyValuePair<SendOrPostCallback, object>>();        

        public override void Send(SendOrPostCallback d, object state) {
            Utils.Log(LogSeverity.Error, nameof(SceneSynchronizationContext), "Sending callback, this is highly unexpected");

            base.Send(d, state);
        }

        public override void Post(SendOrPostCallback d, object state) {
            Utils.Log(LogSeverity.Info, nameof(SceneSynchronizationContext), "Posting callback");
            _queue.Enqueue(new KeyValuePair<SendOrPostCallback, object>(d, state));
        }

        public void ProcessQueueOnCurrentThread() {
            KeyValuePair<SendOrPostCallback, object> item;

            while (_queue.TryDequeue(out item)) {
                item.Key(item.Value);
            }
        }

        public override void OperationStarted() {
            base.OperationStarted();
        }

        public override void OperationCompleted() {
            base.OperationCompleted();
        }

        public override int Wait(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout) {
            return base.Wait(waitHandles, waitAll, millisecondsTimeout);
        }

        public override SynchronizationContext CreateCopy() {
            return base.CreateCopy();
        }
    }
}