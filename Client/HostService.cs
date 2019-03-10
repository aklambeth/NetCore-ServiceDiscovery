using Polly;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestClient
{
    public class HostService
    {
        private IAsyncPolicy policy;

        public HostService()
        {
            policy = Policy.NoOpAsync();
        }

        /// <summary>
        /// Setup our fault handling policy
        /// </summary>
        public AsyncPolicy AsyncPolicy { get => policy as AsyncPolicy; set => policy = value; }

        private void WriteStatUpTag()
        {
            Console.WriteLine("Service Has Started ... ");
            Console.WriteLine("");
            Console.WriteLine("▀▀▀ ▀▀▀▀ ▀▀▀");
        }

        /// <summary>
        /// Start the process running.
        /// </summary>
        /// <param name="exec">Process method</param>
        /// <returns></returns>
        public async Task OnStarting(Func<CancellationToken, Task> exec)
        {
            try
            {
                WriteStatUpTag();
            }
            catch { };

            using (var cts = new CancellationTokenSource())
            {
                Task task = null;
                // Stop the process on ctrl + c
                Console.CancelKeyPress += (sender, args) => OnStopping(cts, task); 
                // Keep the process running untill it is stopped 
                do
                {
                    try
                    {
                        task = policy.ExecuteAsync(exec, cts.Token);
                        await task;
                    }
                    catch { };

                } while (!cts.IsCancellationRequested);
            }
        }

        /// <summary>
        /// Stops the process running
        /// </summary>
        /// <param name="cts"></param>
        /// <param name="task"></param>
        private void OnStopping(CancellationTokenSource cts, Task task)
        {
            Console.WriteLine($"Stopping Process [{task.Id}] ... ");

            // Send cancel signal to background task
            cts.Cancel();

            // And wait for it to stop
            task.GetAwaiter().OnCompleted( () => 
            {
                Console.WriteLine($"Process [{task.Id}] - [STOPPED]");
            });
            
            Console.ReadLine();
        }
    }
}
