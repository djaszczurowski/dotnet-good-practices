using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using GoodPractices.Benchmark.Lib.Http;

namespace GoodPractices.Benchmark.Test.Http
{
  public class HttpTests
  {
    private static HttpClient cli;
    const int length = 1;

    [GlobalSetup]
    public void Setup()
    {
      Cleanup();
      cli = new HttpClient(new HttpClientHandler
      {
        MaxConnectionsPerServer = 1000,
        AutomaticDecompression = System.Net.DecompressionMethods.None
      });
    }


    [GlobalCleanup]
    public void Cleanup()
    {
      if (cli != null)
      {
        cli.Dispose();
      }
      cli = null;
    }

    private long Read(Stream stream)
    {
      long bytes = 0;
      Span<byte> tmp = stackalloc byte[1024];
      var lenght = 0;
      while ((lenght = stream.Read(tmp)) > 0)
      {
        bytes += lenght;
      }
      return bytes;
    }


    [Benchmark]
    public async Task<long> HttpClient_ReadAsStreamAsync()
    {
      long bytes = 0;
      for (int i = 0; i < length; i++)
      {
        using (var response = cli.GetAsync("http://localhost:8080/medium").GetAwaiter().GetResult())
        using (var stream = await response.Content.ReadAsStreamAsync())
        {
          bytes += Read(stream);
        }
      }
      return bytes;
    }

    [Benchmark]
    public async Task<long> HttpClient_ReadAsStreamAsync_ResponseHeadersRead()
    {
      long bytes = 0;
      for (int i = 0; i < length; i++)
      {
        using (var response = cli.GetAsync("http://localhost:8080/medium", HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult())
        {
          using (var stream = await response.Content.ReadAsStreamAsync())
          {
            bytes += Read(stream);
          }
        }
      }
      return bytes;
    }


    [Benchmark]
    public async Task<long> HttpClient_ReadAsStreamAsync_ResponseHeadersRead_ManualDecompression()
    {
      long bytes = 0;
      for (int i = 0; i < length; i++)
      {
        using (var response = cli.GetAsync("http://localhost:8080/medium_gz", HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult())
        using (var stream = await response.Content.ReadAsStreamAsync())
        using (var decompressor = new GZipStream(stream, CompressionMode.Decompress))
        {
          bytes += Read(stream);
        }
      }
      return bytes;
    }

    [Benchmark]
    public async Task<long> HttpClient_LoadIntoBufferAsync_ReadAsStreamAsync_ResponseHeadersRead()
    {
      long bytes = 0;
      for (int i = 0; i < length; i++)
      {
        using (var response = cli.GetAsync("http://localhost:8080/medium", HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult())
        {
          await response.Content.LoadIntoBufferAsync();
          using (var stream = await response.Content.ReadAsStreamAsync())
          {
            bytes += Read(stream);
          }
        }
      }
      return bytes;
    }

  }
}