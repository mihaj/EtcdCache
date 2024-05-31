using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using dotnet_etcd;
using Etcdserverpb;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Caching.Distributed;

namespace EtcdCache;

public class EtcdDistributedCache : IDistributedCache
{
    private readonly EtcdClient _etcdClient;
    private readonly Metadata _metadata;

    public EtcdDistributedCache(EtcdOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }
        
        if (string.IsNullOrEmpty(options.ConnectionString))
        {
            throw new ArgumentNullException("ConnectionString");
        }
        
        _etcdClient = new EtcdClient(options.ConnectionString);

        if (!string.IsNullOrEmpty(options.Username) && !string.IsNullOrEmpty(options.Password))
        {
            var authenticateResponse = _etcdClient.Authenticate(new AuthenticateRequest()
            {
                Name = options.Username, Password = options.Password,
            });

            _metadata = new Metadata() { new("token", authenticateResponse.Token) };
        }
        else
        {
            _metadata = new Metadata();
        }
    }

    /// <summary>
    /// Get key from the Etcd
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public byte[]? Get(string key)
    {
        var response = _etcdClient.GetVal(key, _metadata);

        return string.IsNullOrEmpty(response) ? null : Encoding.UTF8.GetBytes(response);
    }

    /// <summary>
    /// Get key from the Etcd async
    /// </summary>
    /// <param name="key"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public async Task<byte[]?> GetAsync(string key, CancellationToken token = new CancellationToken())
    {
        var response = await _etcdClient.GetValAsync(key, _metadata,
            cancellationToken: token);

        return string.IsNullOrEmpty(response) ? null : Encoding.UTF8.GetBytes(response);
    }

    /// <summary>
    /// Extend a lease for the duration of TTL set initially on the key
    /// </summary>
    /// <param name="key"></param>
    public void Refresh(string key)
    {
        var keyResponse = _etcdClient.Get(new RangeRequest
        {
            Key = ByteString.CopyFromUtf8(key)
        });

        if (keyResponse == null || keyResponse.Kvs == null || !keyResponse.Kvs.Any()) return;

        var leaseId = keyResponse.Kvs.FirstOrDefault().Lease;

        _etcdClient.LeaseKeepAlive(new LeaseKeepAliveRequest() { ID = leaseId },
            method: (resp) => _ = resp,
            new CancellationToken(),
            _metadata).Wait();
    }

    /// <summary>
    /// Extend a lease for the duration of TTL set initially on the key with async method
    /// </summary>
    /// <param name="key"></param>
    /// <param name="token"></param>
    public async Task RefreshAsync(string key, CancellationToken token = new CancellationToken())
    {
        var keyResponse = await _etcdClient.GetAsync(new RangeRequest
        {
            Key = ByteString.CopyFromUtf8(key)
        }, cancellationToken: token);

        if (keyResponse == null || keyResponse.Kvs == null || !keyResponse.Kvs.Any()) return;

        var leaseId = keyResponse.Kvs.FirstOrDefault().Lease;

        await _etcdClient.LeaseKeepAlive(new LeaseKeepAliveRequest() { ID = leaseId },
            method: (resp) => _ = resp,
            cancellationToken: token,
            headers: _metadata);
    }

    /// <summary>
    /// Remove Key from Etcd
    /// </summary>
    /// <param name="key"></param>
    public void Remove(string key)
    {
        _etcdClient.Delete(key, _metadata);
    }

    /// <summary>
    /// Remove key from Etcd async method
    /// </summary>
    /// <param name="key"></param>
    /// <param name="token"></param>
    public async Task RemoveAsync(string key, CancellationToken token = new CancellationToken())
    {
        await _etcdClient.DeleteAsync(key, _metadata, cancellationToken: token);
    }

    /// <summary>
    /// Create key to Etcd
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="options"></param>
    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        double expiryTime = 0;

        if (options.AbsoluteExpiration != null)
        {
            var duration = options.AbsoluteExpiration.Value.Subtract(DateTimeOffset.UtcNow);

            expiryTime = duration.TotalSeconds;
        }

        if (options.SlidingExpiration != null)
        {
            expiryTime = options.SlidingExpiration.Value.TotalSeconds;
        }

        if (options.AbsoluteExpirationRelativeToNow != null)
        {
            expiryTime = options.AbsoluteExpirationRelativeToNow.Value.TotalSeconds;
        }

        var lease = _etcdClient.LeaseGrant(
            new LeaseGrantRequest
            {
                TTL = Convert.ToInt32(expiryTime)
            },
            _metadata);

        _etcdClient.Put(
            new PutRequest
            {
                Key = ByteString.CopyFromUtf8(key),
                Value = ByteString.CopyFromUtf8(Encoding.UTF8.GetString(value)),
                Lease = lease.ID,
            }, _metadata);
    }

    /// <summary>
    /// Create key to Etcd async method
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="options"></param>
    /// <param name="token"></param>
    public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options,
        CancellationToken token = new CancellationToken())
    {
        double expiryTime = 0;

        if (options.AbsoluteExpiration != null)
        {
            var duration = options.AbsoluteExpiration.Value.Subtract(DateTimeOffset.UtcNow);

            expiryTime = duration.TotalSeconds;
        }

        if (options.SlidingExpiration != null)
        {
            expiryTime = options.SlidingExpiration.Value.TotalSeconds;
        }

        if (options.AbsoluteExpirationRelativeToNow != null)
        {
            expiryTime = options.AbsoluteExpirationRelativeToNow.Value.TotalSeconds;
        }

        var lease = await _etcdClient.LeaseGrantAsync(
            new LeaseGrantRequest
            {
                TTL = Convert.ToInt32(expiryTime)
            },
            _metadata,
            cancellationToken: token);

        await _etcdClient.PutAsync(
            new PutRequest
            {
                Key = ByteString.CopyFromUtf8(key),
                Value = ByteString.CopyFromUtf8(Encoding.UTF8.GetString(value)),
                Lease = lease.ID,
            }, _metadata,
            cancellationToken: token);
    }
}