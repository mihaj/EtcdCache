namespace EtcdCache;

public class EtcdOptions
{
    /// <summary>
    /// Etcd connection string
    /// </summary>
    public string ConnectionString { get; set; }
    /// <summary>
    /// If user authentication is enabled set user name
    /// </summary>
    public string? Username { get; set; }
    /// <summary>
    /// If user authentication is enabled set user password
    /// </summary>
    public string? Password { get; set; }
}