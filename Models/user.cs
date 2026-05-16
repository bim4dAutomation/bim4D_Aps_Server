namespace APS_Automation_Server.Models
{
    public class user
    {
        public int Id { get; set; }
        public string Email { get; set; } = null!;
        public string Role { get; set; } = "Viewer";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
