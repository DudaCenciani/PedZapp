    using Microsoft.AspNetCore.Identity;

    namespace PedZapp.Models
    {
        /// <summary>
        /// Usuário do Identity. Usuários de empresa são vinculados por EmpresaId;
        /// administradores master não usam esse vínculo para acessar dados empresariais.
        /// </summary>
        public class ApplicationUser : IdentityUser
        {
            public bool IsAdminMaster { get; set; }

            public int? EmpresaId { get; set; }

            public Empresa? Empresa { get; set; }
        }
    }

