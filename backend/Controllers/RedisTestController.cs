using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Threading.Tasks;

namespace SentimatrixAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RedisTestController : ControllerBase
    {
        private readonly IDistributedCache _cache;

        public RedisTestController(IDistributedCache cache)
        {
            _cache = cache;
        }

        [HttpGet("test")]
        public async Task<IActionResult> TestRedisCache()
        {
            string cacheKey = "test_connection";
            
            // Try to set a value in cache
            await _cache.SetStringAsync(cacheKey, "Redis is working!", 
                new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(10)));

            // Try to retrieve the value
            var cachedValue = await _cache.GetStringAsync(cacheKey);

            return Ok(new 
            { 
                Message = "Redis Connection Test", 
                CachedValue = cachedValue 
            });
        }
    }
}