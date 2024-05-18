using System.Text;

namespace XeonPwm.Driver.Commands;

internal class CommandSerializer
{
    private readonly byte _argSeparator;
    private const char DefaultSeparator = '|';
    
    public CommandSerializer(char argSeparator = DefaultSeparator)
    {
        _argSeparator = (byte)argSeparator;
    }

    public int Serialize(Span<byte> destination, string commandName) => 
        Serialize(destination, commandName, ReadOnlySpan<string>.Empty);

    public int Serialize(Span<byte> destination, string commandName, params string[] args) => 
        Serialize(destination, commandName, args.AsSpan());

    public int Serialize(Span<byte> destination, string commandName, ReadOnlySpan<string> args)
    {
        var offset = Encoding.ASCII.GetBytes(commandName, destination);
        destination[offset++] = _argSeparator;

        foreach (var arg in args)
        {
            offset += Encoding.ASCII.GetBytes(arg, destination[offset..]);
            destination[offset++] = _argSeparator;
        }

        return offset;
    }
}