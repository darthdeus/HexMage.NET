using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using HexMage.Simulator;

namespace HexMage.GUI {
    public static class TaskHelper {
        public static Task ContinueOnGuiThread(this Task task, Action continuation) {
            return task.ContinueWith(_ => continuation(), TaskScheduler.FromCurrentSynchronizationContext());
        }

        public static Task ContinueOnGuiThread<T>(this Task<T> task, Action continuation) {
            return task.ContinueWith(_ => continuation(), TaskScheduler.FromCurrentSynchronizationContext());
        }

        public static Task ContinueOnGuiThread(this Task task, Action<Task> continuation) {
            return task.ContinueWith(continuation, TaskScheduler.FromCurrentSynchronizationContext());
        }

        public static Task ContinueOnGuiThread<T>(this Task<T> task, Action<Task<T>> continuation) {
            return task.ContinueWith(continuation, TaskScheduler.FromCurrentSynchronizationContext());
        }
    }

    public class SceneSynchronizationContext : SynchronizationContext {
        ConcurrentQueue<KeyValuePair<SendOrPostCallback, object>> _queue =
            new ConcurrentQueue<KeyValuePair<SendOrPostCallback, object>>();

        public override void Send(SendOrPostCallback d, object state) {
            Utils.Log(LogSeverity.Error, nameof(SceneSynchronizationContext),
                      "Sending callback, this is highly unexpected");

            base.Send(d, state);
        }

        public override void Post(SendOrPostCallback d, object state) {
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