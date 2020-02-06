using Dapr;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace dapr.sample.webapi.Controllers
{
    [ApiController]
    public class SampleController : ControllerBase
    {
        StateClient stateClient;
        public SampleController([FromServices] StateClient stateClient)
        {
            this.stateClient = stateClient;
        }

        [Topic("hello")]
        [HttpGet("hello")]
        public ActionResult<string> Get()
        {
            Console.WriteLine("Hello, World.");
            return "World";
        }

        const string StateKey = "STATE_KEY";

        [Topic("add")]
        [HttpPost("add")]
        public async Task<ActionResult<int>> Add([FromBody]int x)
        {
            // await stateClient.SaveStateAsync(StateKey, (int?)0);
            var state = await this.stateClient.GetStateEntryAsync<int?>(StateKey);
            state.Value ??= (int?)0;
            state.Value += x;
            await state.SaveAsync();

            Console.WriteLine($"New value = {state.Value}");

            return state.Value;
        }
    }
}