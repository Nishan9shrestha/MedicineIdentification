using Microsoft.EntityFrameworkCore;
using MedicineIdentification.Models;

namespace MedicineIdentification.Data
{
    public class MedicineDbContext : DbContext
    {
        public MedicineDbContext(DbContextOptions<MedicineDbContext> options) : base(options)
        { }

        public DbSet<Medicine> Medicines { get; set; }
        public DbSet<User> Users { get; set; }

    }
}
