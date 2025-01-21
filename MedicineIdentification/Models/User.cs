namespace MedicineIdentification.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; } // Store hashed passwords
        public string Role { get; set; } // "Admin" or "User"
    }
}
