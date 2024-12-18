using MongoDB.Driver;
using SentimatrixAPI.Models;
using SentimatrixAPI.Data;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace SentimatrixAPI.Services
{
    public class EmailService
    {
        private readonly IMongoCollection<Email> _emails;
        private readonly ILogger<EmailService> _logger;
        private readonly IDistributedCache _cache;

        public EmailService(
            IOptions<MongoDBSettings> settings, 
            ILogger<EmailService> logger,
            IDistributedCache cache)
        {
            _logger = logger;
            _cache = cache;
            try
            {
                var client = new MongoClient(settings.Value.ConnectionString);
                var database = client.GetDatabase(settings.Value.DatabaseName);
                _emails = database.GetCollection<Email>(settings.Value.EmailsCollectionName);
                
                _logger.LogInformation($"Successfully connected to MongoDB: {settings.Value.DatabaseName}");
                Console.WriteLine("*************Connected*************");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to connect to MongoDB: {ex.Message}");
                throw;
            }
        }

        // Cached method to get total email count
        public async Task<long> GetTotalEmailCountAsync()
        {
            string cacheKey = "total_email_count";
            
            // Try to get from cache first
            var cachedCount = await _cache.GetStringAsync(cacheKey);
            if (cachedCount != null)
            {
                return long.Parse(cachedCount);
            }

            // If not in cache, get from database
            long totalCount = await _emails.CountDocumentsAsync(FilterDefinition<Email>.Empty);

            // Cache the count for 10 minutes
            var cacheOptions = new DistributedCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(10))
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(30));

            await _cache.SetStringAsync(cacheKey, totalCount.ToString(), cacheOptions);

            return totalCount;
        }

        // Cached method to get emails with optional caching
        public async Task<List<Email>> GetAsync(bool useCache = true)
        {
            string cacheKey = "all_emails";

            if (useCache)
            {
                // Try to get from cache first
                var cachedEmails = await _cache.GetStringAsync(cacheKey);
                if (cachedEmails != null)
                {
                    return JsonSerializer.Deserialize<List<Email>>(cachedEmails);
                }
            }

            // If not in cache or cache disabled, get from database
            var emails = await _emails.Find(_ => true)
                                      .SortByDescending(e => e.Time)
                                      .ToListAsync();

            // Cache the results
            var cacheOptions = new DistributedCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(10))
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(30));

            await _cache.SetStringAsync(
                cacheKey, 
                JsonSerializer.Serialize(emails), 
                cacheOptions
            );

            return emails;
        }

        // Method to clear specific cache entries
        public async Task ClearCacheAsync(string cacheKey)
        {
            await _cache.RemoveAsync(cacheKey);
        }

        public async Task<Email> GetAsync(string id)
        {
            try
            {
                return await _emails.Find(x => x.Id == id).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving email by ID: {ex.Message}");
                throw;
            }
        }

        public async Task CreateAsync(Email email)
        {
            try
            {
                await _emails.InsertOneAsync(email);
                _logger.LogInformation($"Successfully stored email from {email.Sender} with score {email.Score}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating email: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Email>> GetPositiveEmailsAsync()
        {
            try
            {
                return await _emails.Find(x => x.Type == "positive")
                                  .SortByDescending(e => e.Time)
                                  .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving positive emails: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Email>> GetNegativeEmailsAsync()
        {
            try
            {
                return await _emails.Find(x => x.Type == "negative")
                                  .SortByDescending(e => e.Time)
                                  .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving negative emails: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Email>> GetEmailsByScoreRangeAsync(int minScore, int maxScore)
        {
            try
            {
                return await _emails.Find(x => x.Score >= minScore && x.Score <= maxScore)
                                  .SortByDescending(e => e.Time)
                                  .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving emails by score range: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Email>> GetEmailsBySenderAsync(string senderEmail)
        {
            try
            {
                return await _emails.Find(x => x.Sender == senderEmail)
                                  .SortByDescending(e => e.Time)
                                  .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving emails by sender: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateAsync(string id, Email updatedEmail)
        {
            try
            {
                await _emails.ReplaceOneAsync(x => x.Id == id, updatedEmail);
                _logger.LogInformation($"Successfully updated email with ID: {id}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating email: {ex.Message}");
                throw;
            }
        }

        public async Task RemoveAsync(string id)
        {
            try
            {
                await _emails.DeleteOneAsync(x => x.Id == id);
                _logger.LogInformation($"Successfully removed email with ID: {id}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error removing email: {ex.Message}");
                throw;
            }
        }

        public async Task<List<SentimentData>> GetSentimentTrend(PipelineDefinition<EmailData, SentimentData> pipeline)
        {
            // Implement logic to retrieve sentiment trend based on the specified pipeline
            // This is a placeholder implementation
            return new List<SentimentData>();
        }

        public async Task<DashboardStats> GetDashboardStats()
        {
            // Implement logic to retrieve dashboard statistics
            // This is a placeholder implementation
            return new DashboardStats();
        }

        public async Task<DashboardStats> GetDashboardStatsAsync()
        {
            return await GetDashboardStats();
        }
    }
}
