namespace SystemCtrl.Core.Exceptions;

public class ElevatedCommandException(string message, string command, string commandOutput) : Exception(message)
{
    private string Command { get; } = command;
    private string CommandOutput { get; } = commandOutput;

    public override string ToString()
    {
        return $"{base.ToString()}\n\nCommand: {Command}\n\nOutput:\n{CommandOutput}";
    }
}
