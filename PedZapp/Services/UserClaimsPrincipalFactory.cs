using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using PedZapp.Models;
using System.Security.Claims;

namespace PedZapp.Services
{
    /// <summary>
    /// Inclui na identidade autenticada a claim utilizada pelo filtro AdminMasterAuthorizeAttribute.
    /// O vínculo operacional com uma empresa continua sendo conferido no banco por EmpresaId.
    /// </summary>
    public class UserClaimsPrincipalFactory
        : UserClaimsPrincipalFactory<ApplicationUser>
    {
        public UserClaimsPrincipalFactory(
            UserManager<ApplicationUser> userManager,
            IOptions<IdentityOptions> optionsAccessor)
            : base(
                  userManager,
                  optionsAccessor)
        {
        }

        protected override async Task<ClaimsIdentity>
            GenerateClaimsAsync(
                ApplicationUser user)
        {
            var identity =
                await base
                .GenerateClaimsAsync(user);

            identity.AddClaim(
                new Claim(
                    "IsAdminMaster",
                    user.IsAdminMaster.ToString()
                ));

            return identity;
        }
    }
}
