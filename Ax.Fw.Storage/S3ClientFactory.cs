using Amazon.S3;
using Ax.Fw.Extensions;
using Ax.Fw.Storage.S3;
using System.Collections.Concurrent;
using System.Web;

namespace Ax.Fw.Storage;

public static class S3ClientFactory
{
  private static readonly ConcurrentDictionary<string, S3Client> p_cachedS3Clients = new();

  /// <summary>
  /// Creates s3 client from string
  /// 
  /// Format: http(s)://<s3-host>/<bucket>/?api-key=<api-key>&secret-key=<secret-key>&auth-region=<auth-region>
  /// </summary>
  /// <exception cref="UriFormatException"></exception>
  public static S3Client FromString(string _connectionString)
  {
    ArgumentNullException.ThrowIfNullOrWhiteSpace(_connectionString);

    return p_cachedS3Clients.GetOrAdd(_connectionString, _connStr =>
    {
      var url = new Uri(_connectionString);
      var query = HttpUtility.ParseQueryString(url.Query);
      var bucketPath = url.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
      if (bucketPath.Length == 0)
        throw new UriFormatException("Bucket is empty");

      var scheme = url.Scheme;
      var apiKey = query.Get("api-key") ?? throw new UriFormatException("Api key is empty");
      var secret = query.Get("secret-key") ?? throw new UriFormatException("Secret key is empty");
      var authRegion = query.Get("auth-region");
      var host = url.Host;
      if (!host.StartsWith("https://") && !host.StartsWith("http://"))
        host = $"https://{host}";

      var bucket = bucketPath[0];

      var config = new AmazonS3Config()
      {
        ServiceURL = host,
      };
      if (!authRegion.IsNullOrWhiteSpace())
        config.AuthenticationRegion = authRegion;

      var s3Client = new AmazonS3Client(apiKey, secret, config);
      return new S3Client(s3Client, bucket);
    });
  }

}
