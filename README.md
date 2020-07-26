# ParallelForAsyncTest
Showing how Parallel.For doesn't play well with async-await.

This simple C# console application just shows how Parallel.For handles running an async task. 

How to run: 
Clone the repo and dotnet run

The project runs a simple "do work" task in different ways. The do work task will write to an output.log file.

The working task is kicked off n-times in different ways. 

Synchronous: Each time it runs, and waits for the task to complete before running it it again. 
Asynchronous: The tasks are added to a list as they are kicked off, and then it awaits with WhenAll for all the tasks to complete.
Bulkhead: The tasks use Polly Bulkhead to kick off m (<n) tasks at a time, and queue the rest until there is a free spot on the bulkhead to run. 
Parallel.For: The tasks are kicked off inside a Parallel.For. This is the equivalent of the Asynchronous route, except we "expected" parallel.For to await and run them with some concurrency limit. But as we can see, it doesn't. 

If you watch the console output, kicking off the tasks happens slightly differently in each case. 

The results.log file is where it logs the time taken for each type of run, except the parallel.for. 

On my machine:
Bulkhead and Asynchronous were very close.

Parameters: 
MaxFileConcurrent: 	4
Bulkheads: 	10
SubIterations: 	2
SubMs: 	1000

Average times to complete: 
Synchronous:  48101ms
Asynchronous: 16223ms
Bulkhead:     14524ms

