namespace XeonPwm.Driver.Tests.Streams;

internal class FakeReadStream : MemoryStream 
{
    private readonly byte[] _fakeResponse;
    private int _index = 0;

    public FakeReadStream(byte[] fakeResponse)
    {
        _fakeResponse = fakeResponse;
    }
        
    public override int Read(byte[] buffer, int offset, int count)
    {
        if (++_index == 1)
        {
            _fakeResponse.CopyTo(buffer.AsSpan(offset, count));
            return _fakeResponse.Length;
        }
        else
        {
            buffer[offset] = 0;
            return 1;
        }
    }

    public override int Read(Span<byte> buffer)
    {
        if (++_index == 1)
        {
            _fakeResponse.CopyTo(buffer);
            return _fakeResponse.Length;
        }
        else
        {
            buffer[0] = 0;
            return 1;
        }
    }
}