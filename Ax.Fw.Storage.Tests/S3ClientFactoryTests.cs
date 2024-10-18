using Xunit.Abstractions;

namespace Ax.Fw.Storage.Tests;


public class S3ClientFactoryTests
{
  private readonly ITestOutputHelper p_output;

  public S3ClientFactoryTests(ITestOutputHelper _output)
  {
    p_output = _output;
  }

  [Fact]
  public async Task TestConnectionStringAsync()
  {
    var lifetime = new Lifetime();
    try
    {
      var url = "https://s3.eu-west-2.amazonaws.com/bucket_name/folder_path/subpath?api-key=api_key&secret-key=secret_key";
      var s3Client = S3ClientFactory.FromString(url);

      Assert.Equal("folder_path/subpath", s3Client.FolderPath);
    }
    finally
    {
      lifetime.End();
    }
  }
}
