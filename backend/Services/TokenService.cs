using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SmartLearning.Api.Models;

namespace SmartLearning.Api.Services
{
    // Creates the JWT token that the React app sends with every request
    // in the header:  Authorization: Bearer <token>
    public class TokenService
    {
        // Name of the claim that holds the user's SecurityStamp.
        public const string StampClaim = "stamp";

        // Call this when the role or password changes: every older login token stops working.
        public static void RenewSecurityStamp(User user)
        {
            user.SecurityStamp = Guid.NewGuid().ToString("N");
        }

        private readonly IConfiguration _configuration;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string CreateToken(User user)
        {
            // Claims = small pieces of information stored inside the token.
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(StampClaim, user.SecurityStamp ?? "")
            };

            string secretKey = _configuration["Jwt:Key"];
            SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            SigningCredentials credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            int expiryHours = _configuration.GetValue<int>("Jwt:ExpiryHours");

            JwtSecurityToken token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(expiryHours),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
