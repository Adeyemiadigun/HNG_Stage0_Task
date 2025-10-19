namespace HNGTASK.ResponseModels
{
    public record ResponseDto
    {
        public string Status { get; set; } = "success";
        public User User { get; set; } = new User();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Fact { get; set; } = string.Empty;
    }
}