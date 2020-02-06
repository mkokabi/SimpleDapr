# SimpleDapr
There is a [Hello World sample](https://github.com/dapr/samples/tree/master/1.hello-world) in the documentation of Dapr but it's using node and python and persisting the state. While, it's showcasing the capabilities of Dapr but it might be too much for start. 

Here in few step I would present a minimized version:

### Step 1. Creating a dotnet web api and adding Dapr.
```powershell
dotnet new webapi -o dapr.sample.webapi
cd .\dapr.sample.webapi\
dotnet add package Dapr.AspNetCore --version 0.3.0-preview01
```
In startup.cs:
in ConfigureService method

```csharp
            services.AddControllers().AddDapr();
```

and in Configure method before MapControllers()
```csharp
	endpoints.MapSubscribeHandler();
```

As I have another application running on my port 5000. I have to modify my launchSettings.json.
```json
"applicationUrl": "http://localhost:5020"
``` 

Add this controller
```csharp
using Dapr;
using Microsoft.AspNetCore.Mvc;
using System;

namespace dapr.sample.webapi.Controllers
{
    [ApiController]
    public class SampleController : ControllerBase
    {
        [Topic("hello")]
        [HttpGet("hello")]
        public ActionResult<string> Get()
        {
            Console.WriteLine("Hello, World.");
            return "World";
        }
    }
}
```

### Step 2: Running the application
Make sure you are in the project folder. Then run:
```powershell
dapr run --app-id sample --app-port 5020 --port 5030 dotnet run
```
Note: The app-id can be anything but you need it to invoke the methods through Dapr.

Now your application should be running on port 5020 while dapr is running on port 5030.

### Step 3: Testing the application
First let's test the application itself:
```powershell
curl http://127.0.0.1:5020/hello
```
should show in the same window
```powershell
World
```
and in the dapr running window should show:
```
== APP == Hello, World.
```

### Step 5: Testing Dapr
Now we can test dapr
```powershell
curl http://localhost:5030/v1.0/invoke/sample/method/hello
```
which should have the same output in both windows.
the *sample* is the dapr app id and *hello* is the topic.

In addition to the *invoke*, dapr provides 2 other path as *StatePath* and *PublishPath*. 
```csharp
public const string StatePath = "/v1.0/state";
public const string InvokePath = "/v1.0/invoke";
public const string PublishPath = "/v1.0/publish";
```
We would show the former one but the later one is related to Pub/Sub invoke which would be in another post.

## Part 2
Now, we are going to have the state persistent.

Add these to the controller:
```csharp
        const string StateKey = "STATE_KEY";

        [Topic("add")]
        [HttpPost("add")]
        public async Task<ActionResult<int>> Add(
                        [FromBody]int x,
                        [FromServices] StateClient stateClient)
        {
            var state = await stateClient.GetStateEntryAsync<int?>(StateKey);
            state.Value ??= (int?)0;
            state.Value += x;
            await state.SaveAsync();

            return state.Value;
        }
```
restart the dapr and test it with powershell, postman or curl
```powershell
Invoke-WebRequest -Uri 'http://127.0.0.1:5020/add' `
-ContentType application/json `
-Method Post `
-Body 1
```

The output would be like
```
StatusCode        : 200
StatusDescription : OK
Content           : 11
RawContent        : HTTP/1.1 200 OK
                    Transfer-Encoding: chunked
                    Content-Type: application/json; charset=utf-8
                    Date: Thu, 06 Feb 2020 04:47:14 GMT
                    Server: Kestrel

                    11
Forms             : {}
Headers           : {[Transfer-Encoding, chunked], [Content-Type, application/json; charset=utf-8], [Date, Thu, 06 Feb
                    2020 04:47:14 GMT], [Server, Kestrel]}
Images            : {}
InputFields       : {}
Links             : {}
ParsedHtml        : mshtml.HTMLDocumentClass
RawContentLength  : 2
```

Testing the dapr would be similar:
```powershell
Invoke-WebRequest -Uri 'http://localhost:5030/v1.0/invoke/sample/method/add' `
-ContentType application/json `
-Method Post `
-Body 1
```

If we want to only get the state, we can call:
```powershell
Invoke-WebRequest -Uri 'http://localhost:5030/v1.0/state/STATE_KEY' `
-Method Get
```

The *STATE_KEY* is the constant that we defined in the code.

## Dependency Injection
The StateClient which was injected to the method, could be moved to the constructor:
```csharp
        StateClient stateClient;
        public SampleController([FromServices] StateClient stateClient)
        {
            this.stateClient = stateClient;
        }

```
then it should be called with *this*
```csharp
var state = await this.stateClient.GetStateEntryAsync<int?>(StateKey);
```

