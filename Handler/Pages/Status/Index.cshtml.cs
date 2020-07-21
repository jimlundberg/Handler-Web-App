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
    /// <summary>
    /// Index Model class
    /// </summary>
    public class IndexModel : PageModel
    {
        private readonly IStatusRepository statusRepository;

        /// <summary>
        /// Status Data object
        /// </summary>
        public IEnumerable<StatusModels.StatusData> statusData { get; set; }

        /// <summary>
        /// Index Model Constructor
        /// </summary>
        /// <param name="statusRepository"></param>
        public IndexModel(IStatusRepository statusRepository)
        {
            this.statusRepository = statusRepository;
        }

        /// <summary>
        /// On Get
        /// </summary>
        public void OnGet()
        {
            statusData = statusRepository.GetJobStatus().Reverse();
        }
    }
}