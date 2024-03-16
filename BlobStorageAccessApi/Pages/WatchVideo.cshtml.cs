using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BlobStorageAccessApi.Pages
{
    public class WatchVideoModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public string? VideoUrl { get; set; }

        /// <summary>
        /// Initializes the page with the video URL.
        /// </summary>
        /// <param name="fileName">The name of the video file.</param>
        public void OnGet(string fileName)
        {
            // Construct the URL of the video file
            VideoUrl = "/managed-code-bucket/" + fileName;
        }
    }
}