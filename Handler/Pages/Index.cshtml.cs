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
        /// <summary>
        /// ILogger member
        /// </summary>
        public readonly ILogger<IndexModel> _logger;

        /// <summary>
        /// status data
        /// </summary>
        public IEnumerable<StatusWrapper.StatusData> statusData { get; set; }

        /// <summary>
        /// Monitor Data Repository
        /// </summary>
        private readonly IStatusRepository MonitorDataRepository;

        /// <summary>
        /// Ini File Data
        /// </summary>
        public StatusModels.IniFileData iniData { get; set; }

        /// <summary>
        /// Index Model CTOR
        /// </summary>
        /// <param name="monitorDataRepository"></param>
        /// <param name="logger"></param>
        public IndexModel(IStatusRepository monitorDataRepository, ILogger<IndexModel> logger)
        {
            this.MonitorDataRepository = monitorDataRepository;
            _logger = logger;
        }

        /// <summary>
        /// On GEt
        /// </summary>
        public void OnGet()
        {
            statusData = (IEnumerable<StatusWrapper.StatusData>)MonitorDataRepository.GetJobStatus().Reverse();

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
            statusData = (IEnumerable<StatusWrapper.StatusData>)MonitorDataRepository.GetHistoryData().Reverse();
        }
    }
}
