using backend.Models;

namespace backend.Services;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) CreateToken(ApplicationUser user, IList<string> roles);
}
