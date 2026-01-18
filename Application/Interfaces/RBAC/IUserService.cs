using System;
using System.Threading.Tasks;

public interface IUserService
{
    Task<Guid> CreateAsync(CreateUserRequest req);
}
