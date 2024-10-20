using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Diagnostics;
using TPLSample.Services;

namespace TPLSample.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class TestsController : ControllerBase
  {
    private readonly IPdfGenerator pdfGenerator;
    private readonly ILogger<TestsController> logger;
   


    public TestsController(IPdfGenerator pdfGenerator, ILogger<TestsController> logger)
    {
      this.pdfGenerator = pdfGenerator;
      this.logger = logger;
    }


    // 25 adet testi
    // 690
    // 551
    // 576
    // 585

    [HttpPost]
    public async Task<IActionResult> CreatePdfParalel()
    {

      string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Files");

      var list = Enumerable.Range(1, 100).ToList();

      Stopwatch sw = new Stopwatch();
      sw.Start();

      Parallel.ForEach(list, item =>
      {
        this.logger.LogInformation($" ThreadId : {Thread.CurrentThread.ManagedThreadId}");
        this.pdfGenerator.Generate($"{folderPath}/{item}.pdf");
      });

      sw.Stop();
      this.logger.LogInformation("Toplam Süre" + sw.ElapsedMilliseconds);
      

      return Ok();
    }


    // ilk çalıştırmada test doğru değil
    // 999
    // 1021
    // 976
    // 983
    [HttpPost("v3")]
    public async Task<IActionResult> CreatePdfTask()
    {

      string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Files2");

      var list = Enumerable.Range(1, 25).ToList();

      Stopwatch sw = new Stopwatch();
      sw.Start();


      list.ForEach(async item =>
      {

        await this.pdfGenerator.GenerateAsync($"{folderPath}/00{item}.pdf");
        //this.logger.LogInformation($"File : {item} ThreadId : {Thread.CurrentThread.ManagedThreadId}");

      });

      this.logger.LogInformation("Toplam Süre" + sw.ElapsedMilliseconds);



      return Ok();
    }


    [HttpPost("v2")]
    public async Task<IActionResult> CreatePdf()
    {

      string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Files");

      var list = Enumerable.Range(1, 50).ToList();

      Stopwatch sw = new Stopwatch();
      sw.Start();

      list.ForEach(item =>
      {
        this.logger.LogInformation($"ThreadId : {Thread.CurrentThread.ManagedThreadId}");
        this.pdfGenerator.Generate($"{folderPath}/_{item}.pdf");
      });

      sw.Stop();
      this.logger.LogInformation("Toplam Süre" + sw.ElapsedMilliseconds);


      return Ok();
    }


    // Race Condition Sample
    // MultiThread Paylaşımlı veri üzerinde çalışmak
    // 113.432
    [HttpGet("raceCondition")]
    public async Task<IActionResult> RaceCondition()
    {
      string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Files");
      object _lockObject = new object();

      List<FileInfo> fileInfos = new List<FileInfo>();

      long filesByte = 0;
      int filesCount = 0;

      var files =  Directory.GetFiles(folderPath);

      Parallel.ForEach(files, item =>
      {
        this.logger.LogInformation($" ThreadId : {Thread.CurrentThread.ManagedThreadId}");
        FileInfo f = new FileInfo(item);

        // Paylaşımlı veriyi f size kadar arttır.
        // Herhangi bir thread buraya geldiğinde başka bir thread filesByte erişimini engeller.
        // Bunu kullanamazsak aynı anda farklı threadler filesByte değeri üzerinden güncelleme yapmaya çalışır.
        //filesByte += f.Length;
        //filesCount++;
        //fileInfos.Add(f);

        lock (_lockObject)
        {
          filesByte += f.Length;
          filesCount++;
          fileInfos.Add(f);
        }

        // Interlocked.Add(ref filesByte, f.Length); // değeri artırarak git
        // Interlocked.Increment(ref filesCount); // 1 er 1 er artır.

        // Interlocked.Exchange(ref filesByte, 300); // Thread Safe bir şekilde değeri güncelle
        // Interlocked.Decrement(ref filesByte); // değeri 1 azlat

      });


      return Ok(new { filesByte, filesCount, fileInfoSize = fileInfos.Select(x=> x.Name)});
    }


  }
}
