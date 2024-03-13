namespace BluOsNadRemote.App.Extensions;

internal static class ObservableCollectionExtensions
{
    /// <summary>
    /// Replace overlap, Add or remove items
    /// </summary>
    internal static void Fuse<T>(this ObservableCollection<T> target, List<T> incoming)
    {
        var currentCount = target.Count;
        var incomingCount = incoming.Count;
        var max = Math.Max(currentCount, incomingCount);
        var min = Math.Min(currentCount, incomingCount);
        Debug.WriteLine($"current: {currentCount}, incoming: {incomingCount}, max: {max}");

        //replace elements
        for (var i = 0; i < min; i++)
        {
            Debug.WriteLine($"Replaced '{target[i]}' by '{incoming[i]}' {i} to Current");
            target[i] = incoming[i];
        }

        //either add or remove            
        if (incomingCount > currentCount)
        {
            for (var i = min; i < max; i++)
            {
                target.Add(incoming[i]);
                Debug.WriteLine($"Added '{incoming[i]}' {i} to Current");
            }
        }
        else
        {
            for (var i = max; i > min; i--)
            {
                Debug.WriteLine($"Removed '{target[i - 1]}' {i - 1} from Current");
                target.RemoveAt(i - 1);
            }
        }
    }
}
