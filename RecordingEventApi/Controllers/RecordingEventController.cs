using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace YourNamespace.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecordingEventController : ControllerBase
    {
        private readonly ILogger<RecordingEventController> _logger;

        public RecordingEventController(ILogger<RecordingEventController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Post()
        {
            // Log all HTTP headers
            foreach (var header in Request.Headers)
            {
                _logger.LogInformation("Header {Key}: {Value}", header.Key, header.Value);
            }

            // Read and log the request body
            string requestBody;
            using (var reader = new StreamReader(Request.Body))
            {
                requestBody = await reader.ReadToEndAsync();
            }
            _logger.LogInformation("Request Body: {Body}", requestBody);

            try
            {
                // ACS Event Grid events are typically sent as an array.
                var events = JsonSerializer.Deserialize<JsonElement[]>(requestBody);
                if (events != null && events.Length > 0)
                {
                    var firstEvent = events[0];

                    // Check for subscription validation event
                    if (firstEvent.TryGetProperty("eventType", out JsonElement eventTypeElement) &&
                        eventTypeElement.GetString() == "Microsoft.EventGrid.SubscriptionValidationEvent")
                    {
                        if (firstEvent.TryGetProperty("data", out JsonElement dataElement) &&
                            dataElement.TryGetProperty("validationCode", out JsonElement validationCodeElement))
                        {
                            string validationCode = validationCodeElement.GetString();
                            _logger.LogInformation("Subscription validation event received. Validation Code: {Code}", validationCode);
                            return Ok(new { validationResponse = validationCode });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing event payload.");
                return StatusCode(500, "Error processing event payload.");
            }

            return Ok(new { message = "Event received and logged." });
        }
    }
}