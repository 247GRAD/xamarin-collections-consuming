# Installation

Include the package in your Portable Code.

`Install-Package Xamarin.X247Grad.Collections.Consuming -Version 1.0.0`

# Principle

When filling a list view with items that come from an asynchroneous endpoint, it is often desireable to automatically
keep up with the seen items and request more when the list is scrolled to a certain point. For this, an `AsyncCollector`
is provided, extending a standard `ObservableCollection` to provide a way to consume an `IAsyncEnumerable`. This enumerable
can then take care of paging and request cancellation. In addition, `ContinuousRequestsBehavior` captures a list's item
appearing events and translates them to requests to that collection.

# Usage

First, an appropriate generator is needed. This can easily be done by writing an async method returning an `IAsyncEnumerable`.

```cs
private static async IAsyncEnumerable<string> GenerateData([EnumeratorCancellation] CancellationToken cancellationToken = default)
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
```

This method provides a batch of items and then *loads* the next items. To connect it to a list view, it will be passed within an
`AsyncCollector`.

```cs
List.ItemsSource = new AsyncCollector<string>(GenerateData());
```

Within the XAML file of the page, the behavior needs to be attached. First, import the necessary namespace.

```xaml
<ContentPage
  xmlns:behaviors="clr-namespace:Xamarin.X247Grad.Collections.Consuming.Behaviors;assembly=Xamarin.X247Grad.Collections.Consuming">
</ContentPage>
```
The behavior is then attached to the list that it captures the appearing item position of.

```xaml
<ListView>
  <ListView.Behaviors>
      <behaviors:ContinuousRequestsBehavior />
  </ListView.Behaviors>
  <ListView.ItemTemplate>...</ListView.ItemTemplate>
</ListView>
```

With this behavior attached, appearing items will be send as requests to the list's source, if it is currently assigned to satisfy requests.

## Working indicator

The `AsyncCollector` tracks when it is running and publishes that state with the `Working` event. This can be used to indicate loading while
the underlying enumerator produces more items. A connection can for example be established like so.

```cs
// Create source with "Working" handler.
  var source = new AsyncCollector<string>(GenerateData());
  source.Working += (o, working) => Status.IsRunning = working;

  // Assign item source.
  List.ItemsSource = source;
```

For the full example see [ConsumeApi.xaml](Examples/ConsumeApi.xaml) and [
ConsumeApi.xaml.cs](Examples/ConsumeApi.xaml.cs).

## Replacing a source, disposal

After the collection is no longer needed (the page is unloading or the source is replaced), it should be disposed of properly.
A simple way to deal with replacing a source is to track the last assigned source and disposing of it.

```cs
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
```

Depending on the lifecycle of your page, a disposal call should also be placed in the `Disappearing` handler to prevent the underlying
enumerable to keep pending.

For an example of dealing with updated sources see [UpdateRequest.xaml](Examples/UpdateRequest.xaml) and [
UpdateRequest.xaml.cs](Examples/UpdateRequest.xaml.cs).

## Synchronization context

Modification of the actual collection, as well as indication of the working status, needs to happen on the main thread. The constructor will
capture the synchronization context at the time it is invoked. When the collector is created outside of the main thread, the synchronization
context of the main thread should be captured beforehand and then passed as the constructor's second argument. Usually, construction outside
of the main thread will not be necessary.

## Further notes

When using a view template, the list views `CachingStrategy` might need to be set to `RecycleElementAndDataTemplate`.
