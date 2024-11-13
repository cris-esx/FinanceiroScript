namespace FinanceiroScript.Dominio
{
    public class PessoaJuridica
    {
        public Guid Id { get; set; }
        public string RazaoSocial { get; set; }
        public string Cnpj { get; set; }
        public string Email { get; set; }
        public string IncricaoMunicipal { get; set; }
        public string Endereco { get; set; }
        public string Municipio { get; set; }
        public string Cep {  get; set; }

        public PessoaJuridica() { }
    }
}
