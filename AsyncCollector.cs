using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.X247Grad.Collections.Consuming
{
    /// <summary>
    /// Consumes an <see cref="IAsyncEnumerable{T}"/> into the collection when a certain count is requested.
    /// </summary>
    /// <typeparam name="T">The type of the items.</typeparam>
    public class AsyncCollector<T> : ObservableCollection<T>, IRequestCount, IDisposable
    {
        /// <summary>
        /// Lock to prevent multiple tasks from generating.
        /// </summary>
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Source of cancellation to pass to the enumerator so that underlying resources can be stopped.
        /// </summary>
        private readonly CancellationTokenSource _cancellation;

        /// <summary>
        /// The generating enumerator.
        /// </summary>
        private readonly IAsyncEnumerator<T> _generator;

        /// <summary>
        /// The context to run adds and events on.
        /// </summary>
        private readonly SynchronizationContext _syncOn;

        /// <summary>
        /// Handler invoked when a task is working to fulfill a <see cref="Request"/>.
        /// </summary>
        public event EventHandler<bool> Working;

        /// <summary>
        /// Creates the consuming observable from a given <see cref="IAsyncEnumerable{T}"/>.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="syncOn">
        /// The context to run notification of <see cref="Working"/> and mutation of the collection on. If given as
        /// <c>null</c>, <see cref="SynchronizationContext.Current"/> will be captured.</param>
        public AsyncCollector(IAsyncEnumerable<T> source, SynchronizationContext syncOn = null)
        {
            // Create cancellation and generator.
            _cancellation = new CancellationTokenSource();
            _generator = source.GetAsyncEnumerator(_cancellation.Token);

            // Take given or capture current context.
            _syncOn = syncOn ?? SynchronizationContext.Current;
        }

        /// <summary>
        /// Request the collection to be filled up to the requested <paramref name="count"/>, if possible.
        /// </summary>
        /// <param name="count">The count requested.</param>
        public async void Request(int count)
        {
            void AddCallback(object state) => Add((T) state);

            void WorkingCallback(object state) => Working?.Invoke(this, (bool) state);

            // If already satisfied, skip this one.
            if (Count > count)
                return;

            // Queue a new resolution.
            await Task.Run(async () =>
            {
                // Allow only one working thread.
                await _lock.WaitAsync();

                try
                {
                    // Check again in task, another task might have already generated the necessary elements. 
                    if (Count > count)
                        return;

                    // Mark as start working on context.
                    _syncOn.Send(WorkingCallback, true);

                    // While elements need to be filled and available, add on context.
                    while (Count < count && await _generator.MoveNextAsync())
                        _syncOn.Send(AddCallback, _generator.Current);
                }
                catch (OperationCanceledException)
                {
                    // Ok, can occur if underlying enumerator is cancelled.
                }
                finally
                {
                    // Mark as done working on context.
                    _syncOn.Send(WorkingCallback, false);

                    // Release lock.
                    _lock.Release();
                }
            });
        }

        /// <summary>
        /// Cancels the generator and disposes of the acquired resources. 
        /// </summary>
        public async void Dispose()
        {
            // Generate cancellation and dispose the token.
            try
            {
                _cancellation.Cancel();
                _cancellation.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // Ok, already disposed.
            }

            try
            {
                // Dispose generator.
                await _generator.DisposeAsync();
            }
            catch (NotSupportedException)
            {
                // Ok. Some do not implement this.
            }
        }
    }
}