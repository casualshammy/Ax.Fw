using Amazon.S3;
using Amazon.S3.Model;
using System.Runtime.CompilerServices;

namespace Ax.Fw.Storage.S3;

public class S3Client
{
  private readonly AmazonS3Client p_s3Client;
  private readonly string p_bucket;

  internal S3Client(
    AmazonS3Client _client,
    string _bucket,
    string _path)
  {
    p_s3Client = _client;
    p_bucket = _bucket;
    FolderPath = _path.Trim('/');
  }

  public string FolderPath { get; }

  public async Task PullAsync(
    string _key,
    Stream _outputStream,
    CancellationToken _ct)
  {
    var request = new GetObjectRequest
    {
      BucketName = p_bucket,
      Key = _key
    };

    using (var res = await p_s3Client.GetObjectAsync(request, _ct).ConfigureAwait(false))
    using (var resStream = res.ResponseStream)
      await resStream.CopyToAsync(_outputStream, _ct).ConfigureAwait(false);
  }

  public async Task PutAsync(
    string _key,
    Stream _inputStream,
    CancellationToken _ct)
  {
    var req = new PutObjectRequest
    {
      BucketName = p_bucket,
      Key = _key,
      InputStream = _inputStream,
      AutoCloseStream = false,
      AutoResetStreamPosition = false,
    };

    _ = await p_s3Client.PutObjectAsync(req, _ct).ConfigureAwait(false);
  }

  public async Task PutAsync(
    string _key,
    string _inputFilePath,
    CancellationToken _ct)
  {
    var req = new PutObjectRequest
    {
      BucketName = p_bucket,
      Key = _key,
      FilePath = _inputFilePath,
    };

    _ = await p_s3Client.PutObjectAsync(req, _ct).ConfigureAwait(false);
  }

  public async Task<GetObjectMetadataResponse?> GetMetaAsync(
    string _key,
    CancellationToken _ct)
  {
    try
    {
      var result = await p_s3Client.GetObjectMetadataAsync(p_bucket, _key, _ct).ConfigureAwait(false);
      return result;
    }
    catch
    {
      return null;
    }
  }

  public async IAsyncEnumerable<S3Object> ListAsync(
    string? _prefix,
    int _limit,
    [EnumeratorCancellation] CancellationToken _ct)
  {
    var objCounter = 0;

    var req = new ListObjectsV2Request
    {
      BucketName = p_bucket,
      Prefix = _prefix ?? default,
    };

    var res = await p_s3Client.ListObjectsV2Async(req, _ct).ConfigureAwait(false);
    foreach (var obj in res.S3Objects)
    {
      if (obj.Key.EndsWith('/'))
        continue;
      if (objCounter++ > _limit)
        yield break;

      yield return obj;
    }

    while (res?.IsTruncated == true)
    {
      _ct.ThrowIfCancellationRequested();
      req = new ListObjectsV2Request
      {
        BucketName = p_bucket,
        Prefix = _prefix ?? default,
        ContinuationToken = res.NextContinuationToken
      };

      res = await p_s3Client.ListObjectsV2Async(req, _ct).ConfigureAwait(false);
      foreach (var obj in res.S3Objects)
      {
        if (obj.Key.EndsWith('/'))
          continue;
        if (objCounter++ > _limit)
          yield break;

        yield return obj;
      }
    }
  }

  public async Task DeleteAsync(
    string _key,
    CancellationToken _ct)
  {
    var req = new DeleteObjectRequest
    {
      BucketName = p_bucket,
      Key = _key,
    };

    _ = await p_s3Client.DeleteObjectAsync(req, _ct).ConfigureAwait(false);
  }

  public async Task<string> GetPublicLinkAsync(
    string _key,
    DateTime? _expiration = null,
    CancellationToken _ct = default)
  {
    var req = new GetPreSignedUrlRequest
    {
      BucketName = p_bucket,
      Key = _key,
      Expires = _expiration ?? DateTime.Now + TimeSpan.FromDays(6),
      Protocol = Protocol.HTTPS,
      Verb = HttpVerb.GET
    };

    var url = await p_s3Client.GetPreSignedURLAsync(req).ConfigureAwait(false);
    return url;
  }

}
