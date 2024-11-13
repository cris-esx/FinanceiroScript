using FinanceiroScript.Dominio.Interfaces.Helpers;

public class DiretorioHelper : IDiretorioHelper
{
    private string _appRootPath;
    private string _dateTimeString;
    private string _resultDirectory;

    public DiretorioHelper()
    {
        _appRootPath = @"C:\Users\W111\Downloads\TesteAutomateFinance\";
        _dateTimeString = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        _resultDirectory = Path.Combine(_appRootPath, "Resultados", $"Resultado-{_dateTimeString}");

        Directory.CreateDirectory(GetResultDirectory());
        Directory.CreateDirectory(GetValidosDirectory());
        Directory.CreateDirectory(GetErrosDirectory());
    }

    public string GetAppRootPath() => _appRootPath;
    public string GetResultDirectory() => _resultDirectory;
    public string GetValidosDirectory() => Path.Combine(_resultDirectory, "Validos");
    public string GetErrosDirectory() => Path.Combine(_resultDirectory, "Erros");
}
