using Microsoft.AspNetCore.Mvc;

namespace SlowApi.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class SlowController(
        ILogger<SlowController> logger,
        ICacheRequest<SlowRequest> cache,
        SlowService service)
        : ControllerBase
    {
        [HttpPost]
        public IActionResult Post([FromBody] SlowRequest request)
        {
            logger.LogInformation("Request Post accepted Id: {@Id}", request.RequestId);
            _ = service.Process(request);

            return Ok();
        }

        [HttpGet("{requestId}")]
        public IActionResult GetStatus(Guid requestId)
        {
            logger.LogInformation("Request Get accepted Id: {@Id}", requestId);

            var request = cache.Get(x => x.RequestId == requestId);

            return Ok(request.Status);
        }
    }

    public class SlowService(ICacheRequest<SlowRequest> cache)
    {
        public async Task Process(SlowRequest request)
        {
            cache.Add(request);
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(Random.Shared.Next(10, 60)));

                if (DateTime.Now.Minute % 2 == 0)
                {
                    throw new Exception();
                }
            }
            catch
            {
                request.Status = StatusRequest.ERROR;
                cache.Update(request);
                throw;
            }

            request.Status = StatusRequest.SUCESS;
            cache.Update(request);
        }
    }

    public record SlowRequest
    {
        public Guid RequestId { get; set; }
        public StatusRequest Status { get; set; }
    }

    public interface ICacheRequest<T> where T : class
    {
        void Add(T value);

        void Update(T value);

        T Get(Func<T, bool> predicate);
    }

    public class FakeCache : List<SlowRequest>, ICacheRequest<SlowRequest>
    {
        public SlowRequest Get(Func<SlowRequest, bool> predicate)
        {
            return this.FirstOrDefault(predicate, new() { Status = StatusRequest.ERROR });
        }

        public void Update(SlowRequest value)
        {
            var request = this.Single(x => x.RequestId == value.RequestId);

            request.Status = value.Status;
        }
    }

    public enum StatusRequest
    {
        ERROR = -1,
        IN_PROGRESS,
        SUCESS,
    }
}