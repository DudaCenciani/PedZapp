
using PedZapp.Models;
using Microsoft.AspNetCore.Identity;
    using PedZapp.Models;

    namespace PedZapp.Data.Seed
    {
        public static class DbInitializer
        {
            public static async Task SeedAdminAsync(
                IServiceProvider serviceProvider)
            {
                var userManager =
                    serviceProvider
                    .GetRequiredService<
                        UserManager<ApplicationUser>>();

                string email = "admin@pedzapp.com";

                var admin =
                    await userManager
                    .FindByEmailAsync(email);

                if (admin == null)
                {
                    var user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true,
                        IsAdminMaster = true
                    };

                    await userManager.CreateAsync(
                        user,
                        "Admin@123"
                    );
                }

            string empresaEmail =
"empresa@teste.com";

            var empresaUser =
                await userManager
                .FindByEmailAsync(
                    empresaEmail
                );

            if (empresaUser == null)
            {
                var user =
                    new ApplicationUser
                    {
                        UserName =
                            empresaEmail,

                        Email =
                            empresaEmail,

                        EmailConfirmed =
                            true,

                        IsAdminMaster =
                            false
                    };

                await userManager
                    .CreateAsync(
                        user,
                        "Empresa@123"
                    );
            }
        }


        }
    }

