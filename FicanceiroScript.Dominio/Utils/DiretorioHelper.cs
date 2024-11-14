using FinanceiroScript.Dominio.Interfaces.Helpers;

public class DiretorioHelper : IDiretorioHelper
{
    private string _caminhoRaizAplicacao;
    private string _dataHoraString;
    private string _diretorioResultado;

    public DiretorioHelper()
    {
        _caminhoRaizAplicacao = @"C:\Users\W111\Downloads\TesteAutomateFinance\";
        _dataHoraString = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        _diretorioResultado = Path.Combine(_caminhoRaizAplicacao, "Resultados", $"Resultado-{_dataHoraString}");

        Directory.CreateDirectory(ObterDiretorioResultado());
        Directory.CreateDirectory(ObterDiretorioValidos());
        Directory.CreateDirectory(ObterDiretorioErros());
    }

    public string ObterCaminhoRaizAplicacao() => _caminhoRaizAplicacao;
    public string ObterDiretorioResultado() => _diretorioResultado;
    public string ObterDiretorioValidos() => Path.Combine(_diretorioResultado, "Validos");
    public string ObterDiretorioErros() => Path.Combine(_diretorioResultado, "Erros");
}
