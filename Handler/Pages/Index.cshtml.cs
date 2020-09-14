using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Status.Services;
using Status.Models;
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
        public IEnumerable<StatusData> StatusData { get; set; }
        private readonly ILogger<IndexModel> Logger;
        private readonly IStatusRepository MonitorDataRepository;
        private static bool firstTime = true;

        /// <summary>
        /// Index Model CTOR
        /// </summary>
        /// <param name="monitorDataRepository"></param>
        /// <param name="logger"></param>
        public IndexModel(IStatusRepository monitorDataRepository, ILogger<IndexModel> logger)
        {
            MonitorDataRepository = monitorDataRepository;
            Logger = logger;
        }

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
         
        private void SetButtonState(ButtonPress buttonPress)
        {
            // Watch the following settings.  Because disabled=disabled we have to invert the intuitive logic upside down.
            bool bsDisabled = true;
            bool bsEnabled = false;
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
            }

            StatusData = MonitorDataRepository.GetJobStatus().Reverse();
            if (StatusData == null)
            {
                Logger.LogError("OnPostHistoryButton StatusData return null");
            }
        }

        /// <summary>
        /// On Post Home Button
        /// </summary>
        public void OnPostHomeButton()
        {
            SetButtonState(ButtonPress.Home);
            StatusData = MonitorDataRepository.GetJobStatus().Reverse();
            if (StatusData == null)
            {
                Logger.LogError("OnPostHistoryButton StatusData return null");
            }
        }

        /// <summary>
        /// On Post Start Button
        /// </summary>
        public void OnPostStartButton()
        {
            SetButtonState(ButtonPress.Start);
            MonitorDataRepository.StartMonitorProcess();
            StatusData = MonitorDataRepository.GetJobStatus().Reverse();
            if (StatusData == null)
            {
                Logger.LogError("OnPostHistoryButton StatusData return null");
            }
        }

        /// <summary>
        /// On Post Refresh Button
        /// </summary>
        public void OnPostRefreshButton()
        {
            SetButtonState(ButtonPress.Refresh);
            StatusData = MonitorDataRepository.GetJobStatus().Reverse();
            if (StatusData == null)
            {
                Logger.LogError("OnPostHistoryButton StatusData return null");
            }
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
            StatusData = MonitorDataRepository.GetHistoryData();
            if (StatusData == null)
            {
                Logger.LogError("OnPostHistoryButton StatusData return null");
            }
        }
    }
}
