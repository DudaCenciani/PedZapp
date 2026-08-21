using System.ComponentModel.DataAnnotations;

namespace PedZapp.ViewModels.Empresa
{
    public class EmpresaCreateVM
    {
        // Empresa
        [Required]
        public string NomeFantasia { get; set; }
            = string.Empty;

        public string? RazaoSocial { get; set; }

        [Required]
        public string CpfCnpj { get; set; }
            = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; }
            = string.Empty;

        public string? Telefone { get; set; }

        public string? WhatsApp { get; set; }

        public string? Descricao { get; set; }

        public string? Endereco { get; set; }

        public string? Numero { get; set; }

        public string? Bairro { get; set; }

        public string? Cidade { get; set; }

        public string? Estado { get; set; }

        public string? CEP { get; set; }

        // Login
        [Required]
        [DataType(DataType.Password)]
        public string Senha { get; set; }
            = string.Empty;

        [Required]
        [Compare("Senha")]
        [DataType(DataType.Password)]
        public string ConfirmarSenha
        { get; set; }
            = string.Empty;
    }
}
