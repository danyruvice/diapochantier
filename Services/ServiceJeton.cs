using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DiaspoChantier.Models;
using Microsoft.IdentityModel.Tokens;

namespace DiaspoChantier.Services
{
    public class ServiceJeton
    {
        private readonly IConfiguration _configuration;

        public ServiceJeton(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenererJeton(Utilisateur utilisateur, IEnumerable<string> roles)
        {
            var cleSecrete = _configuration["Jwt:Key"];
            var emetteur = _configuration["Jwt:Issuer"];
            var auditoire = _configuration["Jwt:Audience"];
            var duree = int.TryParse(_configuration["Jwt:ExpireMinutes"], out var minutes) ? minutes : 60;

            if (string.IsNullOrWhiteSpace(cleSecrete) || string.IsNullOrWhiteSpace(emetteur) || string.IsNullOrWhiteSpace(auditoire))
            {
                throw new InvalidOperationException("La configuration JWT est incomplète.");
            }

            var cle = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cleSecrete));
            var credentials = new SigningCredentials(cle, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, utilisateur.Id),
                new(ClaimTypes.Name, utilisateur.Email ?? utilisateur.UserName ?? string.Empty),
                new(ClaimTypes.Email, utilisateur.Email ?? string.Empty),
                new("role", utilisateur.Role.ToString())
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var jeton = new JwtSecurityToken(
                issuer: emetteur,
                audience: auditoire,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(duree),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(jeton);
        }
    }
}
