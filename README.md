# EtcdCache
IDistributedCache implementation for Etcd k/v store.

### How to use

Register with services:

```csharp
builder.Services.AddEtcdCache(options =>
{
    options.ConnectionString = "http://localhost:2379";
    options.Username = "root";
    options.Password = "root";
});
```

Inject into class:

```csharp
public class MyClass
{
    private readonly IDistributedCache _distributedCache;

    public MyClass(IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache;
    }

    public async Task<IActionResult> Write(string key, string value)
    {
        //Save to cache
        await _distributedCache.SetAsync(key,
            Encoding.UTF8.GetBytes(value), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            });

        //Read from cache
        var cachedValue = await _distributedCache.GetAsync(key);
    }

```

