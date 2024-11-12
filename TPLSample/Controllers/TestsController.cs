using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using TPLSample.Services;
using System.Collections.Generic;

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

    [HttpPost("v1")]
    public async Task<IActionResult> CreatePdfParalelForeach(CancellationToken token)
    {

      string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Files");

      var list = Enumerable.Range(1, 50000).ToList();

      Stopwatch sw = new Stopwatch();
      sw.Start();

      // sırasız bir işlem gerektiren bir hesaplama durumunda paralel olarak çalışmak mantıklı.

      //Task.Run(() =>
      // {
      //   Parallel.ForEach(list, item =>
      //   {
      //     //this.logger.LogInformation($" ThreadId : {Thread.CurrentThread.ManagedThreadId}");
      //     this.pdfGenerator.Generate($"{folderPath}/{item}.pdf");

      //     Console.Out.WriteLine(item);

      //   });

      // });

      // yukarıdaki Task.Run ile aynı yazım şekli

      await Parallel.ForEachAsync(list, async (item, token) =>
      {
        //this.logger.LogInformation($" ThreadId : {Thread.CurrentThread.ManagedThreadId}");
        await this.pdfGenerator.GenerateAsync($"{folderPath}/{item}.pdf");

      });


      // Parallel.ForEach işlemi Main Thread üzerinden tüm operasyonun sonucunu vermek için Main Thread bloke eder. Ama Parallel.ForEach içerisindeki her bir işlem, paralelde multi thread olarak çalışır.


      sw.Stop();
      this.logger.LogInformation("Toplam Süre" + sw.ElapsedMilliseconds);


      return Ok();
    }


    // ilk çalıştırmada test doğru değil
    // 999
    // 1021
    // 976
    // 983
    [HttpPost("v2")] // Paralel yerine async method üzerinden süreci yönettiğimiz kod bloğu
    public async Task<IActionResult> CreatePdfTask()
    {

      string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Files2");

      var list = Enumerable.Range(1, 25).ToList();

      Stopwatch sw = new Stopwatch();
      sw.Start();


      // foreach içerisinde asenkron bir kod bloğu çalıştırdığımız için yeniden sırayı kaybettik.
      list.ForEach(async item =>
      {
        // bu kod işlemi main thread bloke etmiyor
        await this.pdfGenerator.GenerateAsync($"{folderPath}/00{item}.pdf");
        Console.WriteLine(item);

      });

      this.logger.LogInformation("Toplam Süre" + sw.ElapsedMilliseconds);



      return Ok();
    }


    [HttpPost("v3")] // senkron kod bloğu
    public async Task<IActionResult> CreatePdf()
    {

      string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Files2");

      var list = Enumerable.Range(1, 25).ToList();

      Stopwatch sw = new Stopwatch();
      sw.Start();
      int i = 0;
      list.ForEach(item =>
      {
        // this.logger.LogInformation($"ThreadId : {Thread.CurrentThread.ManagedThreadId}");
        this.pdfGenerator.Generate($"{folderPath}/_{item}.pdf");
        Console.WriteLine(item);
      });

      sw.Stop();
      this.logger.LogInformation("Toplam Süre" + sw.ElapsedMilliseconds);


      return Ok();
    }




    [HttpPost("paralelFor")]
    public async Task<IActionResult> CreatePdfParalelFor(CancellationToken token)
    {

      List<string> names = ["Ali", "Can", "Ahmet", "Mustafa", "Hakan", "Yunus", "Emre"];

      //Parallel.ForAsync(3, names.Count, async (index, token) =>
      //{
      //  this.logger.LogInformation($" ThreadId : {Thread.CurrentThread.ManagedThreadId}");

      //  this.logger.LogInformation($"{names[index]}");

      //});

      Parallel.For(3, names.Count, (index) =>
      {
        this.logger.LogInformation($" ThreadId : {Thread.CurrentThread.ManagedThreadId}");

        this.logger.LogInformation($"{names[index]}");

      });

      return Ok();

    }


    [HttpGet("raceCondition")]
    public async Task<IActionResult> RaceCondition()
    {

      object lockObject = new object();

      var list = Enumerable.Range(0, 1000).ToList();
      // BlockingCollection List generic sınıfının thread safe hali olarak kullanılabiliyor.

      // race condition durumu ortaya çıkmaması için lock işlemini kendi içerisinde yönetiyor.
      BlockingCollection<int> threadSafeCollection = new BlockingCollection<int>();
      // her bir item'ın race condition durumu olmadan listeye alınmasını sağlar.
      // hangi sıra ile işleniyorsa o sıra ile listeye atılma garantisi sağlıyor.
      // sıralı paralel işlemlerde BlockingCollection önerilir.

      // unordered list, işlem sırasında bir önem durumu yoksa, ama race condition çok nadir de olsa oluşabilir.
      ConcurrentBag<int> bags = new ConcurrentBag<int>();

      ConcurrentDictionary<string, int> keyValues = new ConcurrentDictionary<string, int>();
      


      int filesCount = 0;


      Parallel.ForEach(list, item =>
      {
        // this.logger.LogInformation($" ThreadId : {Thread.CurrentThread.ManagedThreadId}");

        // lockObject ile race Condition durumunu ortadan kaldırmak için kilitleme işlemi yaptık.
        // versiyon 1
        //lock (lockObject)
        //{
        //  filesCount++;
        //}

        Interlocked.Increment(ref filesCount); // Increment 1'er 1'er artırır.
        Interlocked.Add(ref filesCount, 5); // +=
        
        bags.Add(item);

      });


      return Ok(new { filesCount, list = bags.ToList() });
    }



    // Race Condition Sample
    // MultiThread Paylaşımlı veri üzerinde çalışmak
    // 113.432
    [HttpGet("raceCondition2")]
    public async Task<IActionResult> RaceCondition2()
    {
      string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Files");
      object _lockObject = new object();

      // Sırasız (unordered) ve çoklu iş parçacığından eşzamanlı erişime izin veren bir koleksiyondur. Ancak, bloklama ve senkronizasyon işlemleri yapmaz.
      // Verilerin sırasız bir şekilde eklenip çıkarılması yeterliyse ve bloklama gereksizse, yani veri üretimi ve tüketimi arasında sıralama veya kontrol gerekli değilse, ConcurrentBag<T> daha uygun bir seçimdir.
      // ConcurrentBag<T> bazı senaryolarda race condition yaratabilir, çünkü koleksiyonun içsel yönetimi sırasında sıra garantisi yoktur.
      ConcurrentBag<FileInfo> fileInfos = new ConcurrentBag<FileInfo>();

      BlockingCollection<FileInfo> fileInfos2 = new BlockingCollection<FileInfo>();
      // Bir iş parçacığı öğe eklerken (üretici), diğer iş parçacığı öğeleri çekerken (tüketici) bloklama mekanizmaları devreye girer.
      // Bu özellik, veri üretimi ve tüketimi arasında kontrollü bir akış sağlar. BlockingCollection<T> kullanarak, bir iş parçacığının bir öğe eklemesini bekleyen başka bir iş parçacığının beklemek zorunda kalması sağlanabilir.
      //  iş parçacıklarının sırasıyla ve uygun bir şekilde veri üretmesini sağlamak için idealdir. Kapasite sınırlamaları ve bloklama gereksinimleri olan durumlarda kullanılır.



      long filesByte = 0;
      int filesCount = 0;

      var files = Directory.GetFiles(folderPath);

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


      return Ok(new { filesByte, filesCount, fileInfoSize = fileInfos.Select(x => x.Name) });
    }



    // Multi-Thread çalıştığımız için Her Thread kendi içerisinde hesaplamasını Thread Safe bir şekilde tutsun istiyoruz.
    // Buda ParalelFor veya ParalelForeach işlemlerinde, her bir işlem için bir kod bloğu çalıştırmak yerine, toplu olarak bir kod bloğu çalıştırdığımızdan dolayı Performans sağlayan bir yöntemdir..

    [HttpGet("threadSafeLocalVariables")]
    public async Task<IActionResult> ThreadSafeLocalVariables()
    {

      var numbers = Enumerable.Range(0, 10).ToList();

      // () => 0 LocalInit
      // (value, loop, total)  value: currentValue, loop : loopState, total: subTotal
      //  (total)  localFinal

      long sum = 0;


      Parallel.ForEach(numbers, () => 0, (value, loop, total) =>
      {
        // Burada Interlock ile her bir işlemde thread lock etmek yerine her bir thread result kendi içinde topladık.
        this.logger.LogInformation($"loop Is Stoped: {loop.IsStopped}");
        this.logger.LogInformation($"Thread Id {Thread.CurrentThread.ManagedThreadId}");
        total += value;
        return total;

      }, (total) =>
      {
        // Thread Kendi içerisinde hesaplama yaptıktan sonra veritabanına kayıt atsın düşünülebilir.
        // İlla shared Data ile çalışacak diye bir kaide yok.
        Interlocked.Add(ref sum, total); // threadler arasında race condition oluşmasın diye
      });


      return Ok(sum);
    }

    // paralel olarak başlayan bir ifade de iptal işlemlerini nasıl yapacağız.
    // Cancelation Token üzerinden iptal edebiliriz.

    // Paralel.Foreach veya For kullanımında Main thread üzerinde işlemi başlatacak şekilde yazarsak, UI kitleyebiliriz.  Paralel.Foreach içerisindeki kodlar Multi-Thread çalışır fakat, kendi çağırısı kodu blocklamasın diye ya Async yada Task.Run içerisinde çalıştırılabilir.


    [HttpGet("cancelationToken")]
    public async Task<IActionResult> ParalelForCancelation(CancellationToken cancellationToken)
    {
      List<string> urls = ["https://www.google.com", "https://neominal.com"];

      var httpclient = new HttpClient();

      string content = "";


      var options = new ParallelOptions();
      options.CancellationToken = cancellationToken;


      try
      {

        // CPU-bound işler için uygundur ve senkron çalışır.
        // Hesaplama gücüne dayanan işlemleri ifade eder.
        // Bu tür işlemler, işlemcinin yoğun bir şekilde çalıştığı ve genellikle zaman alıcı matematiksel veya mantıksal işlemler içerir.
        //Parallel.ForEach<string>(urls, options, async (url) =>
        //{
        //  // kodu bloklayacak şekilde yazdık. Aynı thread kullanılacak.
        //  content =  httpclient.GetStringAsync(url,cancellationToken).Result;
        //  options.CancellationToken.ThrowIfCancellationRequested();
        //  // eğer işlem iptal edilirse hata fırlat
        //  //this.logger.LogInformation(content);

        //  this.logger.LogInformation($"Parallel ForEach Item Thread {Thread.CurrentThread.ManagedThreadId}");

        //});


        // Asenkron işlemlerle daha verimli bir şekilde çalışarak IO-bound işler için tasarlanmıştır.
        // veri okuma veya yazma işlemlerinin, işlem süresinin büyük bir kısmını oluşturduğu durumları ifade eder. Bu tür işlemler genellikle disk erişimi, ağ bağlantıları veya kullanıcı girişleri gibi yavaş kaynaklarla ilgilidir
        await Parallel.ForEachAsync<string>(urls, options, async (url, CancellationToken) =>
        {
          content = await httpclient.GetStringAsync(url, CancellationToken);
          options.CancellationToken.ThrowIfCancellationRequested();
          // eğer işlem iptal edilirse hata fırlat
          //this.logger.LogInformation(content);

          this.logger.LogInformation($"Parallel ForEachAsync Item Thread {Thread.CurrentThread.ManagedThreadId}");

        });

      }
      catch (OperationCanceledException ex) // OperationCancelException
      {
        logger.LogInformation(ex.Message);

      }

      return Ok(content);
    }


  }
}
