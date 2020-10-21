using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.X247Grad.Collections.Consuming.Examples
{
    public partial class ConsumeApi
    {
        /// <summary>
        /// Generates the data. The async enumerable supports suspension from the yield statements, as well as async
        /// operations in it's body.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token, provided automatically by the compiler.</param>
        /// <returns>Returns the data source.</returns>
        private static async IAsyncEnumerable<string> GenerateData(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // Iterate all pages.
            foreach (var page in Enumerable.Range(0, 10000))
            {
                // For each page, return their items.
                foreach (var item in Enumerable.Range(0, 20))
                    yield return $"Item {item}, page {page}";

                // Simulate loading the next page.
                await Task.Delay(3000, cancellationToken);
            }
        }

        public ConsumeApi()
        {
            InitializeComponent();
        }

        private void LoadData(object sender, EventArgs e)
        {
            // Create source with "Working" handler.
            var source = new AsyncCollector<string>(GenerateData());
            source.Working += (o, working) => Status.IsRunning = working;

            // Assign item source.
            List.ItemsSource = source;
        }
    }
}