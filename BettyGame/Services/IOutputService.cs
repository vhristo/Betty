namespace BettyGame.Services;

public interface IOutputService
{
    void Write(string message);
    void WriteLine(string message);
    void WriteInfo(string message);
    void WriteInfoVerbose(string message);
    void WriteError(string message);
    void WriteSuccess(string message);
    void WriteWarning(string message);
}