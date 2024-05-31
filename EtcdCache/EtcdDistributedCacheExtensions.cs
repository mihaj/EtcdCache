using System;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

namespace EtcdCache;

public static class EtcdDistributedCacheExtensions
{
    public static void AddEtcdCache(this IServiceCollection services, Action<EtcdOptions> options)
    {
        var etcdOptions = new EtcdOptions();
        options?.Invoke(etcdOptions);

        services.AddSingleton<IDistributedCache, EtcdDistributedCache>(
            provider => new EtcdDistributedCache(etcdOptions));
    }
}