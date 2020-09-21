using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Handler.Pages
{
    /// <summary>
    /// Response Cache Configuration
    /// </summary>
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "impactlevel", "pii" })]

    /// <summary>
    /// Error Model Class
    /// </summary>
    public class ErrorModel : PageModel
    {
        /// <summary>
        /// Requet ID
        /// </summary>
        public string RequestId { get; set; }

        /// <summary>
        /// Show Request Id
        /// </summary>
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        private readonly ILogger<ErrorModel> _logger;

        /// <summary>
        /// Error Model logger
        /// </summary>
        /// <param name="logger"></param>
        public ErrorModel(ILogger<ErrorModel> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// On Get
        /// </summary>
        public void OnGet()
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        }
    }
}
