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
        public readonly ILogger<IndexModel> Logger;

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
        /// Button Press
        /// </summary>
        public enum ButtonPress
        {
            /// <summary>
            /// Home Button
            /// </summary>
            Home = 1,

            /// <summary>
            /// Start Button
            /// </summary>
            Start = 2,

            /// <summary>
            /// Refresh Button
            /// </summary>
            Refresh = 3,

            /// <summary>
            /// Pause Button
            /// </summary>
            Pause = 4,

            /// <summary>
            /// Stop Button
            /// </summary>
            Stop = 5,

            /// <summary>
            /// History Button
            /// </summary>
            History = 6 
        };
         
        /// <summary>
        /// Index Model CTOR
        /// </summary>
        /// <param name="monitorDataRepository"></param>
        /// <param name="logger"></param>
        public IndexModel(IStatusRepository monitorDataRepository, ILogger<IndexModel> logger)
        {
            this.MonitorDataRepository = monitorDataRepository;
            Logger = logger;
        }

        private void SetButtonState(ButtonPress buttonPress)
        {
            // Watch the following settings.  Because disabled=disabled we have to invert the intuitive logic upside down.
            Boolean bsDisabled = true;
            Boolean bsEnabled = false;
            ViewData["bsStartDisabled"] = bsEnabled;
            ViewData["bsRefreshDisabled"] = bsEnabled;
            ViewData["bsPauseDisabled"] = bsEnabled;
            ViewData["bsStopDisabled"] = bsEnabled;

            switch (buttonPress)
            {
                case ButtonPress.Home:
                    ViewData["PageName"] = "Home";
                    ViewData["bsRefreshDisabled"] = bsDisabled;
                    ViewData["bsPauseDisabled"] = bsDisabled;
                    ViewData["bsStopDisabled"] = bsDisabled;
                    break;

                case ButtonPress.Start:
                    ViewData["PageName"] = "Start";
                    ViewData["bsStartDisabled"] = bsDisabled;
                    break;

                case ButtonPress.Stop:
                    ViewData["PageName"] = "Stop";
                    ViewData["bsRefreshDisabled"] = bsDisabled;
                    ViewData["bsPauseDisabled"] = bsDisabled;
                    ViewData["bsStopDisabled"] = bsDisabled;
                    break;

                case ButtonPress.Pause:
                    ViewData["PageName"] = "Pause";
                    ViewData["bsRefreshDisabled"] = bsDisabled;
                    ViewData["bsPauseDisabled"] = bsDisabled;
                    ViewData["bsStopDisabled"] = bsDisabled;
                    break;

                case ButtonPress.Refresh:
                    ViewData["PageName"] = "Refresh";
                    ViewData["bsStartDisabled"] = bsDisabled;
                    break;

                case ButtonPress.History:
                    ViewData["PageName"] = "History";
                    ViewData["bsStartDisabled"] = bsDisabled;
                    ViewData["bsPauseDisabled"] = bsDisabled;
                    ViewData["bsStopDisabled"] = bsDisabled;
                    break;

                default:
                    ViewData["PageName"] = "Home";
                    break;
            }
        }

        /// <summary>
        /// On Get
        /// </summary>
        public void OnGet()
        {
            SetButtonState(ButtonPress.Home);            
            if (firstTime)
            {
                MonitorDataRepository.GetIniFileData();
                MonitorDataRepository.CheckLogFileHistory();
                firstTime = false;
            }
            else
            {
                SetButtonState(ButtonPress.Start);
                MonitorDataRepository.CheckLogFileHistory();
            }
            statusData = (IEnumerable<StatusData>)MonitorDataRepository.GetJobStatus().Reverse();
        }
                
        /// <summary>
        /// On Post Home Button
        /// </summary>
        public void OnPostHomeButton()
        {
            SetButtonState(ButtonPress.Home);
            statusData = (IEnumerable<StatusData>)MonitorDataRepository.GetJobStatus().Reverse();
        }

        /// <summary>
        /// On Post Start Button
        /// </summary>
        public void OnPostStartButton()
        {
            SetButtonState(ButtonPress.Start);
            MonitorDataRepository.StartMonitorProcess();
            statusData = (IEnumerable<StatusData>)MonitorDataRepository.GetJobStatus().Reverse();
        }

        /// <summary>
        /// On Post Refresh Button
        /// </summary>
        public void OnPostRefreshButton()
        {
            SetButtonState(ButtonPress.Refresh);
            statusData = (IEnumerable<StatusData>)MonitorDataRepository.GetJobStatus().Reverse();
        }

        /// <summary>
        /// On Post Pause Button
        /// </summary>
        public void OnPostPauseButton()
        {
            SetButtonState(ButtonPress.Pause);
            MonitorDataRepository.PauseMonitor();
        }

        /// <summary>
        /// On Post Stop Button
        /// </summary>
        public void OnPostStopButton()
        {
            SetButtonState(ButtonPress.Stop);
            MonitorDataRepository.StopMonitor();
        }

        /// <summary>
        /// On Post History Button
        /// </summary>
        public void OnPostHistoryButton()
        {
            SetButtonState(ButtonPress.History);
            statusData = (IEnumerable<StatusData>)MonitorDataRepository.GetHistoryData();
        }
    }
}
