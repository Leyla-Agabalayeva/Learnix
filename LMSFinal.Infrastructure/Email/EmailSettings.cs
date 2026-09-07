namespace LMSFinal.Infrastructure.Email
{

    public class EmailSettings
    {
        public string SmtpHost { get; set; } = "smtp.gmail.com";

        public int SmtpPort { get; set; } = 587;
        public string SenderEmail { get; set; } = string.Empty;

  
        public string SenderPassword { get; set; } = string.Empty;

        public string SenderName { get; set; } = "Learnix";

 
        public string ClientBaseUrl { get; set; } = "http://localhost:5204";
    }
}
