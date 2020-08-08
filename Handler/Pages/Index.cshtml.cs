using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Status.Services;
using StatusModels;
using System;
using System.Collections.Generic;
using System.Linq;

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
        public IEnumerable<StatusData> statusData { get; set; }

        /// <summary>
        /// Monitor Data Repository
        /// </summary>
        private readonly IStatusRepository MonitorDataRepository;
        private static bool firstTime = true;

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
        /// On Get
        /// </summary>
        public void OnGet()
        {
            ViewData["PageName"] = "Home";
            if (firstTime)
            {
                MonitorDataRepository.GetIniFileData();
                MonitorDataRepository.CheckLogFileHistory();
                firstTime = false;
            }
            statusData = (IEnumerable<StatusData>)MonitorDataRepository.GetJobStatus().Reverse();
        }
                
        /// <summary>
        /// On Post Home Button
        /// </summary>
        public void OnPostHomeButton()
        {
            ViewData["PageName"] = "Home";
            statusData = (IEnumerable<StatusData>)MonitorDataRepository.GetJobStatus().Reverse();
        }

        /// <summary>
        /// On Post Start Button
        /// </summary>
        public void OnPostStartButton()
        {
            ViewData["PageName"] = "Start";
            Console.WriteLine("\nStart Button pressed");
            MonitorDataRepository.StartMonitorProcess();
            statusData = (IEnumerable<StatusData>)MonitorDataRepository.GetJobStatus().Reverse();
        }

        /// <summary>
        /// On Post Refresh Button
        /// </summary>
        public void OnPostRefreshButton()
        {
            ViewData["PageName"] = "Refresh";
            Console.WriteLine("\nRefresh Button pressed");
            statusData = (IEnumerable<StatusData>)MonitorDataRepository.GetJobStatus().Reverse();
        }

        /// <summary>
        /// On Post Pause Button
        /// </summary>
        public void OnPostPauseButton()
        {
            ViewData["PageName"] = "Pause";
            Console.WriteLine("\nPause Button pressed");
            statusData = (IEnumerable<StatusData>)MonitorDataRepository.GetJobStatus().Reverse();
        }

        /// <summary>
        /// On Post Stop Button
        /// </summary>
        public void OnPostStopButton()
        {
            ViewData["PageName"] = "Stop";
            Console.WriteLine("\nStop Button pressed");
            MonitorDataRepository.StopMonitor();
        }

        /// <summary>
        /// On Post History Button
        /// </summary>
        public void OnPostHistoryButton()
        {
            ViewData["PageName"] = "History";
            Console.WriteLine("\nHistory Button pressed");
            statusData = (IEnumerable<StatusData>)MonitorDataRepository.GetHistoryData().Reverse();
        }
    }
}
