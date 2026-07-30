using FashionHub.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace FashionHub.Web.Controllers
{
    public class ChatController : Controller
    {
        private readonly IChatAiService _chatAiService;

        public ChatController(IChatAiService chatAiService)
        {
            _chatAiService = chatAiService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetResponse(string userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
                return Json(new { success = false, response = "Chào bạn, OWE có thể giúp gì cho bạn?" });

            try
            {
                string response = await _chatAiService.GetResponseAsync(userMessage);
                return Json(new { success = true, response });
            }
            catch (Exception)
            {
                return Json(new { success = false, response = "Hệ thống đang bận, bạn thử lại sau chút nhé!" });
            }
        }
    }
}
