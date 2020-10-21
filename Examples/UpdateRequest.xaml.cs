using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Xamarin.X247Grad.Collections.Consuming.Examples
{
    public partial class UpdateRequest
    {
        /// <summary>
        /// Generates the data with a specified title.
        /// </summary>
        /// <param name="caption">The title of the item.</param>
        /// <param name="cancellationToken">The cancellation token, provided automatically by the compiler.</param>
        /// <returns>Returns the data source.</returns>
        private static async IAsyncEnumerable<string> GenerateData(string caption,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // Iterate all pages.
            foreach (var page in Enumerable.Range(0, 10000))
            {
                // For each page, return their items.
                foreach (var item in Enumerable.Range(0, 20))
                    yield return $"{caption} {item}, page {page}";

                // Simulate loading the next page.
                await Task.Delay(3000, cancellationToken);
            }
        }

        private AsyncCollector<string> _lastSource;

        public UpdateRequest()
        {
            InitializeComponent();
        }

        private void LoadData(object sender, EventArgs e)
        {
            UpdateCaption(this, new TextChangedEventArgs(string.Empty, Caption.Text));
        }

        private void UpdateCaption(object sender, TextChangedEventArgs e)
        {
            // Stop and dispose the old source, preventing it from keeping it's resources open.
            _lastSource?.Dispose();

            // Create new source with "Working" handler.
            var source = new AsyncCollector<string>(GenerateData(e.NewTextValue));
            source.Working += (o, working) => Status.IsRunning = working;

            // Assign item source.
            List.ItemsSource = _lastSource = source;
        }
    }
}