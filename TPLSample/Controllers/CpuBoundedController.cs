using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace TPLSample.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class CpuBoundedController : ControllerBase
  {

    [HttpGet("V1")]
    public async Task<IActionResult> WithParallel()
    {
      var items = Enumerable.Range(1, 10000000).ToList();

      ConcurrentBag<double> results = new();

      Stopwatch sw = new Stopwatch();

      sw.Start();

      Parallel.ForEach(items, (item) =>
      {
        var val = Math.Sqrt(item);
        results.Add(Math.Sqrt(val));
      });

      sw.Stop();

      Console.Out.WriteLine("Result in ms =>" + sw.ElapsedMilliseconds + " - TotalCount => " + results.Count);

      return Ok();
    }


    [HttpGet("V2")]
    public async Task<IActionResult> WithParallelBlocking()
    {
      var items = Enumerable.Range(1, 10000000).ToList();

      // bu arkadaş sıralaı bir şekilde kodu bloklayarak işlem yaptığından ConcurrentBag göre daha yavaş çalıştı.
      BlockingCollection<double> results = new();

      Stopwatch sw = new Stopwatch();

      sw.Start();

      Parallel.ForEach(items, (item) =>
      {
        var val = Math.Sqrt(item);
        results.Add(Math.Sqrt(val));
      });

      sw.Stop();

      Console.Out.WriteLine("Result in ms =>" + sw.ElapsedMilliseconds + " - TotalCount => " + results.Count);

      return Ok();
    }


    [HttpGet("V3")]
    public async Task<IActionResult> WithoutParallel()
    {
      var items = Enumerable.Range(1, 10000000).ToList();

      // List 10000000 kaydı işleyemedeğinden multi-thread olmasa dahi ConcurrentBag kullanıldı.
      ConcurrentBag<double> results = new();

      Stopwatch sw = new Stopwatch();

      sw.Start();

      items.ForEach((item) =>
      {
        var val = Math.Sqrt(item);
        results.Add(Math.Sqrt(val));
      });

      sw.Stop();

      Console.Out.WriteLine("Result in ms =>" + sw.ElapsedMilliseconds + " - TotalCount => " + results.Count);

      return Ok();
    }
  }
}
