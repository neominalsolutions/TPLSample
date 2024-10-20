namespace TPLSample.Services
{
  public interface IPdfGenerator
  {
    void Generate(string fileName);

    Task GenerateAsync(string fileName);
  }
}
