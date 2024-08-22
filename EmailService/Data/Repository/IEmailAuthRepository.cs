namespace EmailService.Data.Repository;

// CRUD 
public interface IEmailAuthRepository
{
    public IEnumerable<EmailAuthEntity> GetEntities(Func<string, bool>? emailFilter = null, Func<Guid, bool>? verificationTokenFilter = null, Func<DateTime, bool>? verificationTimeFilter = null, Func<bool, bool>? isValidFilter= null);

    public Task RemoveEntity(Func<string, bool>? emailFilter = null, Func<Guid, bool>? verificationTokenFilter = null,
        Func<DateTime, bool>? verificationTimeFilter = null, Func<bool, bool>? isValidFilter = null);
    
    public Task<EmailAuthEntity> AddEntity(string email);
    
    public Task<EmailAuthEntity> UpdateEntity(EmailAuthEntity entity, string? email = null, Guid? verificationToken = null, DateTime? verificationTime = null, bool? isValid = null);
}