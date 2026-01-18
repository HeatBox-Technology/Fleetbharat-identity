using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public class UserService : IUserService
{
    private readonly IdentityDbContext _db;

    public UserService(IdentityDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> CreateAsync(CreateUserRequest req)
    {
        var email = req.Email.Trim().ToLower();

        var exists = await _db.Users.AnyAsync(x => x.Email == email);
        if (exists)
            throw new InvalidOperationException("Email already exists");

        // ✅ validate account
        var accExists = await _db.Accounts.AnyAsync(x => x.AccountId == req.AccountId);
        if (!accExists)
            throw new KeyNotFoundException("Account not found");

        // ✅ validate role must belong to same account
        var roleExists = await _db.Roles.AnyAsync(x => x.RoleId == req.RoleId && x.AccountId == req.AccountId);
        if (!roleExists)
            throw new BadHttpRequestException("Role not valid for this account");

        var user = new User
        {
            UserId = Guid.NewGuid(), // ✅ important

            Email = email,
            FirstName = req.FirstName.Trim(),
            LastName = req.LastName.Trim(),

            Password_hash = BCrypt.Net.BCrypt.HashPassword(req.Password),

            AccountId = req.AccountId,
            roleId = req.RoleId,

            CreatedAt = DateTime.UtcNow,
            Status = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return user.UserId;
    }

}
