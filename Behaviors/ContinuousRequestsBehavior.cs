using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xamarin.Forms;

namespace Xamarin.X247Grad.Collections.Consuming.Behaviors
{
    /// <summary>
    /// <para>
    /// For a <see cref="ListView"/> that has or will have <see cref="IRequestCount"/> item sources, generates requests
    /// based on the visible items and on first assignment. When a <see cref="IRequestCount"/> source was assigned or is
    /// being set, <see cref="Initial"/> items are requested. If an item appears, <see cref="Ahead"/> more items are
    /// requested from the <see cref="IRequestCount"/>. If the item source is not <see cref="IRequestCount"/>, nothing
    /// happens.
    /// </para>
    /// <para>
    /// The generated requests are monotonous after the initial request.
    /// </para>
    /// </summary>
    public class ContinuousRequestsBehavior : Behavior<ListView>
    {
        /// <summary>
        /// Last requested value per list view.
        /// </summary>
        private readonly ConditionalWeakTable<ListView, object> _table = new ConditionalWeakTable<ListView, object>();

        /// <summary>
        /// Property for <see cref="Initial"/>.
        /// </summary>
        public static readonly BindableProperty InitialProperty = BindableProperty.Create(
            nameof(Initial), typeof(int), typeof(ContinuousRequestsBehavior), 10);

        /// <summary>
        /// Property for <see cref="Ahead"/>.
        /// </summary>
        public static readonly BindableProperty AheadProperty = BindableProperty.Create(
            nameof(Ahead), typeof(int), typeof(ContinuousRequestsBehavior), 5);

        /// <summary>
        /// The initial request posted to any <see cref="IRequestCount"/> updated into the item source of the list.
        /// </summary>
        public int Initial
        {
            get => (int) GetValue(InitialProperty);
            set => SetValue(InitialProperty, value);
        }

        /// <summary>
        /// The amount of items to request ahead of an appearing item.
        /// </summary>
        public int Ahead
        {
            get => (int) GetValue(AheadProperty);
            set => SetValue(AheadProperty, value);
        }

        protected override void OnAttachedTo(ListView bindable)
        {
            // Add listeners.
            bindable.PropertyChanged += PostInitial;
            bindable.ItemAppearing += RequestMore;

            // Request initial, if item source was already assigned.
            Request(bindable, Initial, true);
        }

        protected override void OnDetachingFrom(ListView bindable)
        {
            // Remove listeners.
            bindable.ItemAppearing -= RequestMore;
            bindable.PropertyChanged -= PostInitial;
        }

        /// <summary>
        /// Posts the request to the index, applies step sizes.
        /// </summary>
        /// <param name="listView">The target.</param>
        /// <param name="index">The requested index.</param>
        /// <param name="resetTable">If true, resets the last count for the list view.</param>
        private void Request(ListView listView, int index, bool resetTable)
        {
            // Work on list views that have item sources satisfying count requesting.
            if (!(listView.ItemsSource is IRequestCount requestCount))
                return;

            // If table reset is needed (e.g., initialization or new source), reset the entry.
            if (resetTable)
                _table.Remove(listView);

            // Get old entry.
            var last = _table.TryGetValue(listView, out var value) && value is int existing ? existing : 0;

            // If new count is higher than existing value, request new count.
            if (last < index)
            {
                // Update assignment.
                _table.AddOrUpdate(listView, index);

                // Request the count.
                requestCount.Request(index);
            }
        }

        /// <summary>
        /// Translates appearing item indices to count requests.
        /// </summary>
        /// <param name="sender">The sender, should be the list view.</param>
        /// <param name="e">The appearing arguments.</param>
        private void RequestMore(object sender, ItemVisibilityEventArgs e)
        {
            // Assert list type.
            if (!(sender is ListView listView))
                return;

            // Request ahead.
            Request(listView, e.ItemIndex + Ahead, false);
        }

        /// <summary>
        /// Translates changes to the item source to count requests.
        /// </summary>
        /// <param name="sender">The sender, should be the list view.</param>
        /// <param name="e">The property changed arguments.</param>
        private void PostInitial(object sender, PropertyChangedEventArgs e)
        {
            // Make sure the property in question is the item source property.
            if (e.PropertyName != ListView.ItemsSourceProperty.PropertyName)
                return;

            // Assert list type.
            if (!(sender is ListView listView))
                return;

            // Request initial count, reset table.
            Request(listView, Initial, true);
        }
    }
}