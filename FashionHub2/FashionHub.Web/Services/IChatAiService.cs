namespace FashionHub.Web.Services
{
    public interface IChatAiService
    {
        Task<string> GetResponseAsync(string userMessage, int? userId = null);
    }
}