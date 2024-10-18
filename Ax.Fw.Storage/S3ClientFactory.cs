using Amazon.S3;
using Ax.Fw.Storage.S3;
using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Web;

namespace Ax.Fw.Storage;

public static class S3ClientFactory
{
  private static readonly ConcurrentDictionary<string, S3Client> p_cachedS3Clients = new();

  /// <summary>
  /// Creates s3 client from string
  /// 
  /// Format: http(s)://<s3-host>/<bucket>/<folder-path-in-bucket>?api-key=<api-key>&secret-key=<secret-key>
  /// </summary>
  /// <exception cref="UriFormatException"></exception>
  public static S3Client FromString(string _connectionString)
  {
    return p_cachedS3Clients.GetOrAdd(_connectionString, _connStr =>
    {
      var url = new Uri(_connStr);

      var query = HttpUtility.ParseQueryString(url.Query);

      var bucketPath = url.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
      if (bucketPath.Length == 0)
        throw new UriFormatException("Bucket is empty");

      var apiKey = query.Get("api-key") ?? throw new UriFormatException("Api key is empty");
      var secret = query.Get("secret-key") ?? throw new UriFormatException("Secret key is empty");
      var bucket = bucketPath[0];
      var path = string.Join('/', bucketPath.Skip(1));

      var settings = new AmazonS3Config()
      {
        ServiceURL = $"{url.Scheme}://{url.Host}:{url.Port}",
      };

      var s3Client = new AmazonS3Client(apiKey, secret, settings);
      return new S3Client(s3Client, bucket, path);
    });
  }

}
