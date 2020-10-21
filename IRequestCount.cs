namespace Xamarin.X247Grad.Collections.Consuming
{
    /// <summary>
    /// Can serve requests to a desired count.
    /// </summary>
    public interface IRequestCount
    {
        /// <summary>
        /// Requests the given <paramref name="count"/> from the receiver.
        /// </summary>
        /// <param name="count">The count to request.</param>
        void Request(int count);
    }
}