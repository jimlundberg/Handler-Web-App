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
        private readonly ILogger<IndexModel> _logger;
        public string Message { get; set; }

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            Message = "Modeling Handler " + DateTime.Now.ToLocalTime();
        }
        public void OnPostStartButton(int sessionCount)
        {
            Console.WriteLine("Start Button pressed");
        }
        public void OnPostStopButton(int sessionCount)
        {
            Console.WriteLine("Stop Button pressed");
        }
        public void OnPostHistoryButton(int sessionCount)
        {
            Console.WriteLine("History Button pressed");
        }
    }
}
