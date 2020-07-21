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
        private readonly IStatusRepository MonitorDataRepository;
        public StatusModels.IniFileData iniData { get; set; }

        public IndexModel(IStatusRepository monitorDataRepository)
        {
            this.MonitorDataRepository = monitorDataRepository;
        }

        public void OnGet()
        {
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
