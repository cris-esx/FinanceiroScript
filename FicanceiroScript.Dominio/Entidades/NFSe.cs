using FinanceiroScript.Dominio;

namespace FinanceiroScript.Dominio
{
    public class NFSe
    {
        public string ChaveAcesso { get; set; }
        public string Numero { get; set; }
        public string DataCompetencia { get; set; }
        public string DataEmissao { get; set; }
        public PessoaJuridica Prestador { get; set; } = new PessoaJuridica();
        public PessoaJuridica Tomador { get; set; } = new PessoaJuridica();
        public string CodigoServico { get; set; }
        public string DescricaoServico { get; set; }
        public string StatusImpostoMunicipal { get; set; }
        public string IncidenciaMunicipal { get; set; }
        public string ValorServico { get; set; }
        public string ValorLiquidoNotaFiscal { get; set; }

        public NFSe() { }
    }
}
