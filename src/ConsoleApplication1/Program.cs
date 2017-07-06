using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using System.Diagnostics;
using System.Net.Http;
using Polly.CircuitBreaker;

namespace ConsoleApplication1
{
    class Program
    {
        public static void Execute()
        {
            var random = new Random();
            Console.WriteLine(MethodBase.GetCurrentMethod().DeclaringType.Name);
            Console.WriteLine("=======");
            // Let's call a web api service to make repeated requests to a server. 
            // The service is programmed to fail after 3 requests in 5 seconds.

            var client = new WebClient();
            int eventualSuccesses = 0;
            int retries = 0;
            int capture = 0;
            int eventualFailuresDueToCircuitBreaking = 0;
            int eventualFailuresForOtherReasons = 0;

            // Define our CircuitBreaker policy: Break if the action fails 4 times in a row.
            var circuitBreakerPolicy = Policy
                .Handle<Exception>()
                .CircuitBreaker(1, //backoff after every fail
                    TimeSpan.FromSeconds(0.1 * Math.Pow(2, capture)), //backoff algorithm
                    (ex, breakDelay) =>
                    {
                        Console.WriteLine(".Breaker logging: Breaking the circuit for " + breakDelay.TotalMilliseconds + "ms!");
                        Console.WriteLine("..due to: " + ex.Message);
                        retries++;
                        capture = retries;
                        if (retries > 5) //capture effect after it retries 5 times
                        {
                            capture = 0;
                        }
                    },
                    onReset: () => Console.WriteLine(".Breaker logging: Call ok! Closed the circuit again!"),
                    onHalfOpen: () => Console.WriteLine(".Breaker logging: Half-open: Next call is a trial!")
                   
                );

            int i = 0;
            // Do the following until a key is pressed
            while (!Console.KeyAvailable)
            {
                i++;
                Stopwatch watch = new Stopwatch();
                watch.Start();

                try
                {
                    // Retry the following call according to the policy - 3 times.
                    string msg = circuitBreakerPolicy.Execute<String>(() => // Note how we can also Execute() a Func<TResult> and pass back the value.
                    {
                        // This code is executed within the circuitBreakerPolicy 

                        // Make a request and get a response
                        return client.DownloadString("http://localhost:57209" + "/api/values/" + i);
                    });

                    watch.Stop();

                    // Display the response message on the console
                    Console.Write("Response : " + msg);
                    Console.WriteLine(" (after " + watch.ElapsedMilliseconds + "ms)");

                    eventualSuccesses++;
                }
                catch (BrokenCircuitException b)
                {
                    watch.Stop();
                    Console.Write("Request " + i + " failed with: " + b.GetType().Name);
                    Console.WriteLine(" (after " + watch.ElapsedMilliseconds + "ms)");
                    eventualFailuresDueToCircuitBreaking++;
                }
                catch (Exception e)
                {
                    watch.Stop();
                    Console.Write("Request " + i + " eventually failed with: " + e.Message);
                    Console.WriteLine(" (after " + watch.ElapsedMilliseconds + "ms)");
                    eventualFailuresForOtherReasons++;
                }

                // Wait half second
                Thread.Sleep(500);
            }

            Console.WriteLine("");
            Console.WriteLine("Total requests made                     : " + i);
            Console.WriteLine("Requests which eventually succeeded     : " + eventualSuccesses);
            Console.WriteLine("Retries made to help achieve success    : " + retries);
            Console.WriteLine("Requests failed early by broken circuit : " + eventualFailuresDueToCircuitBreaking);
            Console.WriteLine("Requests which failed after longer delay: " + eventualFailuresForOtherReasons);

        }
    }
}
