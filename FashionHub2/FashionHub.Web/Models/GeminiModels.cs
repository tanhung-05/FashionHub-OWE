namespace FashionHub.Web.Models
{
    public class GeminiRequest
    {
        public List<Content> contents { get; set; } = new();
    }

    public class Content
    {
        public List<Part> parts { get; set; } = new();
        public string? role { get; set; }
    }

    public class Part
    {
        public string text { get; set; } = string.Empty;
    }

    public class GeminiResponse
    {
        public List<Candidate>? candidates { get; set; }
    }

    public class Candidate
    {
        public Content? content { get; set; }
    }
}