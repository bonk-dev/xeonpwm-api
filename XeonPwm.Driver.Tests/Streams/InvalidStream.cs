namespace XeonPwm.Driver.Tests.Streams;

internal class InvalidStream : Stream
{
    private readonly bool _readable;
    private readonly bool _writeable;

    public override bool CanRead => _readable;
    public override bool CanWrite => _writeable;
        
    public override bool CanSeek => throw new InvalidOperationException();
    public override long Length => throw new InvalidOperationException();

    public override long Position
    {
        get => throw new InvalidOperationException();
        set => throw new InvalidOperationException();
    }

    public InvalidStream(bool readable, bool writeable)
    {
        _readable = readable;
        _writeable = writeable;
    }
        
    public override void Flush() => throw new InvalidOperationException();
    public override int Read(byte[] buffer, int offset, int count) => throw new InvalidOperationException();
    public override long Seek(long offset, SeekOrigin origin) => throw new InvalidOperationException();
    public override void SetLength(long value) => throw new InvalidOperationException();
    public override void Write(byte[] buffer, int offset, int count) => throw new InvalidOperationException();
}