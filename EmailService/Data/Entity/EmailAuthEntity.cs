using Microsoft.EntityFrameworkCore;

namespace EmailService.Data;

public class EmailAuthEntity
{
    public int Id { get; set; }
    public string Email { get; set; }
    public Guid VerificationToken { get; set; }
    public DateTime VerificationTime { get; set; }
    public bool IsInvalid { get; set; } = false;
}

public class EmailAuthEntityDbContext : DbContext
{
    public EmailAuthEntityDbContext(DbContextOptions<EmailAuthEntityDbContext> options) : base(options)
    {
        
    }
    
    public DbSet<EmailAuthEntity> EmailAuthEntities { get; set; }
}