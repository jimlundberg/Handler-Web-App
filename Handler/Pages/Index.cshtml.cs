using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using Status.Services;
using StatusModels;

namespace Handler.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        public string Message { get; set; }

        private readonly IStatusRepository statusMonitorRepository;

        public IEnumerable<StatusMonitorData> statusMonitorData { get; set; }

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public IndexModel(IStatusRepository monitorDataRepository)
        {
            this.statusMonitorRepository = monitorDataRepository;
        }

        public void OnGet()
        {
            Message = "Modeling Handler time: " + DateTime.Now.ToLocalTime();
            statusMonitorData = statusMonitorRepository.GetMonitorStatus();
        }
    }
}
