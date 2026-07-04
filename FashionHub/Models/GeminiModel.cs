using System.Collections.Generic;

namespace FashionHub.Models
{
    // Cấu trúc gửi đi
    public class GeminiRequest
    {
        public List<Content> contents { get; set; }
    }

    public class Content
    {
        public List<Part> parts { get; set; }
        public string role { get; set; } // "user" hoặc "model"
    }

    public class Part
    {
        public string text { get; set; }
    }

    // Cấu trúc nhận về
    public class GeminiResponse
    {
        public List<Candidate> candidates { get; set; }
    }

    public class Candidate
    {
        public Content content { get; set; }
    }
}