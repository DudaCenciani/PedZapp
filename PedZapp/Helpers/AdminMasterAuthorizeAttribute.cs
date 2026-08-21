using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace PedZapp.Helpers
{
    /// <summary>
    /// Garante que áreas do Administrador Master sejam acessadas somente por uma identidade
    /// autenticada com a claim <c>IsAdminMaster</c>, emitida pela UserClaimsPrincipalFactory.
    /// </summary>
    public class AdminMasterAuthorizeAttribute
        : ActionFilterAttribute
    {
        public override void OnActionExecuting(
            ActionExecutingContext context)
        {
            var user =
                context.HttpContext.User;

            if (user.Identity?.IsAuthenticated != true)
            {
                context.Result = new ChallengeResult();
                return;
            }

            var isAdmin =
                user.FindFirstValue(
                    "IsAdminMaster"
                );

            if (!bool.TryParse(isAdmin, out var isAdminMaster) ||
                !isAdminMaster)
            {
                context.Result = new ForbidResult();
            }
        }
    }
}
