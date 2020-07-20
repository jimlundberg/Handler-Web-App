using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Status.Services;
using StatusModels;

namespace Handler.Pages.Config
{
    public class SetupModel : PageModel
    {
        private readonly IStatusRepository MonitorDataRepository;
        public StatusModels.IniFileData iniData { get; set; }

        public SetupModel(IStatusRepository monitorDataRepository)
        {
            this.MonitorDataRepository = monitorDataRepository;
        }

        public void OnGet()
        {
            iniData = MonitorDataRepository.GetMonitorStatus();
        }
    }
}