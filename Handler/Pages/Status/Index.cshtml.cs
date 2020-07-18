using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Status.Services;
using StatusModels;

namespace Handler.Pages.Status
{
    public class IndexModel : PageModel
    {
        private readonly IStatusRepository statusRepository;
        public IEnumerable<StatusModels.StatusData> statusData { get; set; }

        public IndexModel(IStatusRepository statusRepository)
        {
            this.statusRepository = statusRepository;
        }

        public void OnGet()
        {
            statusData = statusRepository.GetJobStatus().Reverse();
        }
    }
}