using System.Collections.Concurrent;
using Integration.Common;
using Integration.Backend;

namespace Integration.Service;

public sealed class ItemIntegrationService
{
    private ConcurrentDictionary<string, bool> _itemCache = new ConcurrentDictionary<string, bool>();

    //This is a dependency that is normally fulfilled externally.
    private ItemOperationBackend ItemIntegrationBackend { get; set; } = new();

    // This is called externally and can be called multithreaded, in parallel.
    // More than one item with the same content should not be saved. However,
    // calling this with different contents at the same time is OK, and should
    // be allowed for performance reasons.
    public Result SaveItem(string itemContent)
    {
        // To prevent another thread from trying to add the same content at the same time.
        if (!_itemCache.TryAdd(itemContent, true))
            return new Result(false, $"Another request is processing the item with content {itemContent}.");

        // Check if content already exists.
        if (ItemIntegrationBackend.FindItemsWithContent(itemContent).Count != 0)
            return new Result(false, $"Item with content {itemContent} already exists.");

        try
        {
            // Save new item.
            var item = ItemIntegrationBackend.SaveItem(itemContent);
            return new Result(true, $"Item with content {itemContent} saved with id {item.Id}");
        }
        finally
        {
            // When the saving process is complete, we remove the content from the cache.
            _itemCache.TryRemove(itemContent, out _);
        }
    }

    public List<Item> GetAllItems()
    {
        return ItemIntegrationBackend.GetAllItems();
    }
}