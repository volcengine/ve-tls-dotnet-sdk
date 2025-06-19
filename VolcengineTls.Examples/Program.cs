using System;

namespace VolcengineTls.Examples
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Starting example...");

                var example = new ProducerExample();

                example.Run(); // 同步方法
                // example.RunAsync().Wait(); // 异步方法

                Console.WriteLine("Example completed successfully.");
            }
            catch (AggregateException ex)
            {
                foreach (var innerEx in ex.InnerExceptions)
                {
                    Console.WriteLine($"Error: {innerEx.Message}");
                    if (innerEx.InnerException != null)
                    {
                        Console.WriteLine($"Inner Error: {innerEx.InnerException.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Error: {ex.InnerException.Message}");
                }
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
