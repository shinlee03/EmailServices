namespace EmailService.Data.Repository;
public class EmailAuthRepository(EmailAuthEntityDbContext dbContext) : IEmailAuthRepository
{
    private readonly EmailAuthEntityDbContext _context = dbContext;

    public IEnumerable<EmailAuthEntity> GetEntities(Func<string, bool>? emailFilter = null, Func<Guid, bool>? verificationTokenFilter = null, Func<DateTime, bool>? verificationTimeFilter = null, Func<bool, bool>? isValidFilter= null)
    {
        var query = _context.EmailAuthEntities.AsEnumerable();
        return GetMatchingEntities(query, 
            emailFilter: emailFilter, 
            verificationTokenFilter: verificationTokenFilter,
            verificationTimeFilter: verificationTimeFilter,
            isValidFilter: isValidFilter);
    }

    public async Task RemoveEntity(Func<string, bool>? emailFilter = null, Func<Guid, bool>? verificationTokenFilter = null, Func<DateTime, bool>? verificationTimeFilter = null,
        Func<bool, bool>? isValidFilter = null)
    {
        var query = _context.EmailAuthEntities.AsEnumerable();
        query = GetMatchingEntities(query, 
            emailFilter: emailFilter, 
            verificationTokenFilter: verificationTokenFilter,
            verificationTimeFilter: verificationTimeFilter,
            isValidFilter: isValidFilter);
        
        _context.RemoveRange(query);
        await _context.SaveChangesAsync();
    }

    public async Task<EmailAuthEntity> AddEntity(string email)
    {
        var authorizationCode = Guid.NewGuid();
        var emailAuthenticationEntity = new EmailAuthEntity
        {
            Email = email,
            VerificationToken = authorizationCode,
            VerificationTime = DateTime.UtcNow,
            IsInvalid = false
        };
        await _context.EmailAuthEntities.AddAsync(emailAuthenticationEntity);
        await _context.SaveChangesAsync();
        return emailAuthenticationEntity;
    }

    public async Task<EmailAuthEntity> UpdateEntity(EmailAuthEntity entity, string? email = null, Guid? verificationToken = null,
        DateTime? verificationTime = null, bool? isValid = null)
    {
        entity.Email = email ?? entity.Email;
        entity.VerificationToken = verificationToken ?? entity.VerificationToken;
        entity.VerificationTime = verificationTime ?? entity.VerificationTime;
        entity.IsInvalid = isValid ?? entity.IsInvalid;
        
        await _context.SaveChangesAsync();
        return entity;
    }

    private IEnumerable<EmailAuthEntity> GetMatchingEntities(IEnumerable<EmailAuthEntity> query, Func<string, bool>? emailFilter = null, Func<Guid, bool>? verificationTokenFilter = null, Func<DateTime, bool>? verificationTimeFilter = null,
        Func<bool, bool>? isValidFilter = null) =>  query .Where(e => 
            (emailFilter == null || emailFilter(e.Email) )&& 
            (verificationTokenFilter == null || verificationTokenFilter(e.VerificationToken)) &&
            (verificationTimeFilter == null || verificationTimeFilter(e.VerificationTime)) &&
            (isValidFilter == null || isValidFilter(e.IsInvalid)));
}