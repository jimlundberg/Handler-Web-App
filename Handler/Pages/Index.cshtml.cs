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
    /// <summary>
    /// Index Model Class
    /// </summary>
    public class IndexModel : PageModel
    {
        //private readonly ILogger<IndexModel> _logger;

        private readonly IStatusRepository MonitorDataRepository;
        /// <summary>
        /// Ini File Data
        /// </summary>
        public StatusModels.IniFileData iniData { get; set; }

        //public IndexModel(ILogger<IndexModel> logger)
        //{
        //    _logger = logger;
        //}

        /// <summary>
        /// Index Model CTOR
        /// </summary>
        /// <param name="monitorDataRepository"></param>
        public IndexModel(IStatusRepository monitorDataRepository)
        {
            this.MonitorDataRepository = monitorDataRepository;
        }

        /// <summary>
        /// On GEt
        /// </summary>
        public void OnGet()
        {
            //_logger.LogTrace("Log Trace");
            //_logger.LogDebug("Log Debug");
            //_logger.LogInformation("Log Information");
            //_logger.LogError("Log Error");
            //_logger.LogCritical("Log Critical");
        }

        /// <summary>
        /// On Post Start Button
        /// </summary>
        public void OnPostStartButton()
        {
            Console.WriteLine("Start Button pressed");
            iniData = MonitorDataRepository.GetMonitorStatus();
        }

        /// <summary>
        /// On Post Stop Button
        /// </summary>
        public void OnPostStopButton()
        {
            Console.WriteLine("Stop Button pressed");
            MonitorDataRepository.StopMonitor();
        }

        /// <summary>
        /// On Post History Button
        /// </summary>
        public void OnPostHistoryButton()
        {
            Console.WriteLine("History Button pressed");
        }
    }
}
