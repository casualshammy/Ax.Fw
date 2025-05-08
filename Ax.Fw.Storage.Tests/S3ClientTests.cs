namespace Ax.Fw.Storage.Tests;

public class S3ClientTests
{
  [Fact]
  public void TestS3ClientFactoryPath0()
  {
    var connStr = "https://s3.amazonaws.com/bucket/folder/?api-key=key&secret-key=secret&auth-region=us-west-2";
    var s3Client = S3ClientFactory.FromString(connStr);
    Assert.NotNull(s3Client);
    Assert.Equal("bucket", s3Client.Bucket);
    Assert.Equal("folder", s3Client.FolderPath);
  }

  [Fact]
  public void TestS3ClientFactoryPath1()
  {
    var connStr = "https://s3.amazonaws.com/bucket/?api-key=key&secret-key=secret&auth-region=us-west-2";
    var s3Client = S3ClientFactory.FromString(connStr);
    Assert.NotNull(s3Client);
    Assert.Equal("bucket", s3Client.Bucket);
    Assert.Null(s3Client.FolderPath);
  }

  [Fact]
  public void TestS3ClientFactoryPath2()
  {
    var connStr = "https://s3.amazonaws.com/bucket/folder/subfolder/subsubfolder/?api-key=key&secret-key=secret&auth-region=us-west-2";
    var s3Client = S3ClientFactory.FromString(connStr);
    Assert.NotNull(s3Client);
    Assert.Equal("bucket", s3Client.Bucket);
    Assert.Equal("folder/subfolder/subsubfolder", s3Client.FolderPath);
  }

}
