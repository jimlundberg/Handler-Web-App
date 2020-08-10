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
            /// Home
            /// </summary>
            Home = 1, 

            /// <summary>
            /// Start
            /// </summary>
            Start = 2,
            
            /// <summary>
            /// Refresh
            /// </summary>
            Refresh = 3,

            /// <summary>
            /// Pause
            /// </summary>
            Pause = 4, 

            /// <summary>
            /// Stop
            /// </summary>
            Stop = 5, 

            /// <summary>
            /// History
            /// </summary>
            History = 6 
        };

        /// <summary>
        /// Button State
        /// </summary>
        public enum ButtonState 
        {
            /// <summary>
            /// Primary
            /// </summary>
            Primary = 1, 

            /// <summary>
            /// Secondary On
            /// </summary>
            SecondaryOn = 2, 

            /// <summary>
            /// Secondary Off
            /// </summary>
            SecondaryOff = 3 
        };

        /// <summary>
        /// Button State Home
        /// </summary>
        public ButtonState bsHome { get; set; }

        /// <summary>
        /// Button State Start
        /// </summary>
        public ButtonState bsStart { get; set; }

        /// <summary>
        /// Button State bsRefresh
        /// </summary>
        public ButtonState bsRefresh { get; set; }

        /// <summary>
        /// Button State Pause
        /// </summary>
        public ButtonState bsPause { get; set; }

        /// <summary>
        /// Button State Stop
        /// </summary>
        public ButtonState bsStop { get; set; }

        /// <summary>
        /// Button State History
        /// </summary>
        public ButtonState bsHistory { get; set; }

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
            // Based on the button pressed, set the classes of the other buttons to control which actions are enabled
            String bsPrimary = "btn btn-primary";
            String bsSecondaryOn = "btn btn-primary";
            String bsSecondaryOff = "btn btn-primary";
            // Watch the following settings.  Because disabled=disabled we have to invert the intuitive logic
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
                    ViewData["bsHome"] = bsPrimary;
                    ViewData["bsStart"] = bsPrimary; 
                    ViewData["bsRefresh"] = bsSecondaryOff;
                    ViewData["bsPause"] = bsSecondaryOff;
                    ViewData["bsStop"] = bsSecondaryOff;
                    ViewData["bsHistory"] = bsPrimary; 
                    ViewData["bsRefreshDisabled"] = bsDisabled;
                    ViewData["bsPauseDisabled"] = bsDisabled;
                    ViewData["bsStopDisabled"] = bsDisabled;
                    break;

                case ButtonPress.Start:
                    ViewData["PageName"] = "Start";
                    ViewData["bsHome"] = bsSecondaryOn;
                    ViewData["bsStart"] = bsSecondaryOff;
                    ViewData["bsRefresh"] = bsSecondaryOn;
                    ViewData["bsPause"] = bsSecondaryOn;
                    ViewData["bsStop"] = bsPrimary;
                    ViewData["bsHistory"] = bsSecondaryOn;
                    ViewData["bsStartDisabled"] = bsDisabled;
                    break;

                case ButtonPress.Stop:
                    ViewData["PageName"] = "Stop";
                    ViewData["bsHome"] = bsPrimary;
                    ViewData["bsStart"] = bsPrimary; 
                    ViewData["bsRefresh"] = bsSecondaryOff;
                    ViewData["bsPause"] = bsSecondaryOff;
                    ViewData["bsStop"] = bsSecondaryOff;
                    ViewData["bsHistory"] = bsPrimary; 
                    ViewData["bsRefreshDisabled"] = bsDisabled;
                    ViewData["bsPauseDisabled"] = bsDisabled;
                    ViewData["bsStopDisabled"] = bsDisabled;
                    break;

                case ButtonPress.Pause:
                    ViewData["PageName"] = "Pause";
                    ViewData["bsHome"] = bsPrimary; 
                    ViewData["bsStart"] = bsPrimary;
                    ViewData["bsRefresh"] = bsSecondaryOff;
                    ViewData["bsPause"] = bsSecondaryOff;
                    ViewData["bsStop"] = bsSecondaryOff;
                    ViewData["bsHistory"] = bsPrimary; 
                    ViewData["bsRefreshDisabled"] = bsDisabled;
                    ViewData["bsPauseDisabled"] = bsDisabled;
                    ViewData["bsStopDisabled"] = bsDisabled;
                    break;

                case ButtonPress.Refresh:
                    ViewData["PageName"] = "Refresh";
                    ViewData["bsHome"] = bsPrimary; 
                    ViewData["bsStart"] = bsSecondaryOff;
                    ViewData["bsRefresh"] = bsPrimary;
                    ViewData["bsPause"] = bsPrimary; 
                    ViewData["bsStop"] = bsPrimary; 
                    ViewData["bsHistory"] = bsPrimary; 
                    break;

                case ButtonPress.History:
                    ViewData["PageName"] = "History";
                    ViewData["bsHome"] = bsPrimary; 
                    ViewData["bsStart"] = bsPrimary; 
                    ViewData["bsRefresh"] = bsSecondaryOff;
                    ViewData["bsPause"] = bsSecondaryOff;
                    ViewData["bsStop"] = bsSecondaryOff;
                    ViewData["bsHistory"] = bsPrimary;
                    ViewData["bsRefreshDisabled"] = bsDisabled;
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
            statusData = (IEnumerable<StatusData>)MonitorDataRepository.GetJobStatus().Reverse();
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
            statusData = (IEnumerable<StatusData>)MonitorDataRepository.GetHistoryData().Reverse();
        }
    }
}
