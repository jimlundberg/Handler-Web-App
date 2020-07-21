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
        //private readonly ILogger<IndexModel> _logger;

        private readonly IStatusRepository MonitorDataRepository;
        public StatusModels.IniFileData iniData { get; set; }

        //public IndexModel(ILogger<IndexModel> logger)
        //{
        //    _logger = logger;
        //}

        public IndexModel(IStatusRepository monitorDataRepository)
        {
            this.MonitorDataRepository = monitorDataRepository;
        }

        public void OnGet()
        {
            //_logger.LogTrace("Log Trace");
            //_logger.LogDebug("Log Debug");
            //_logger.LogInformation("Log Information");
            //_logger.LogError("Log Error");
            //_logger.LogCritical("Log Critical");
        }

        public void OnPostStartButton()
        {
            Console.WriteLine("Start Button pressed");
            iniData = MonitorDataRepository.GetMonitorStatus();
        }

        public void OnPostStopButton()
        {
            Console.WriteLine("Stop Button pressed");
            MonitorDataRepository.StopMonitor();
        }

        public void OnPostHistoryButton()
        {
            Console.WriteLine("History Button pressed");
        }
    }
}
