using EfCore.Enterprise.Shared.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace EfCore.Enterprise.Infrastructure.Services;

public interface IIdGeneratorService
{
    long NextId();
    string NextIdString();
}

public class SnowflakeIdGenerator : IIdGeneratorService
{
    private readonly long _workerId;
    private readonly long _datacenterId;
    private long _sequence = 0L;
    private long _lastTimestamp = -1L;

    private readonly object _lock = new();

    private const long Twepoch = 1288834974657L;
    private const long WorkerIdBits = 5L;
    private const long DatacenterIdBits = 5L;
    private const long MaxWorkerId = -1L ^ (-1L << (int)WorkerIdBits);
    private const long MaxDatacenterId = -1L ^ (-1L << (int)DatacenterIdBits);
    private const long SequenceBits = 12L;
    private const long SequenceMask = -1L ^ (-1L << (int)SequenceBits);

    private const long WorkerIdShift = SequenceBits;
    private const long DatacenterIdShift = SequenceBits + WorkerIdBits;
    private const long TimestampLeftShift = SequenceBits + WorkerIdBits + DatacenterIdBits;

    public SnowflakeIdGenerator(IConfiguration configuration)
    {
        _workerId = configuration.GetValue<long>("Snowflake:WorkerId", 1);
        _datacenterId = configuration.GetValue<long>("Snowflake:DatacenterId", 1);

        if (_workerId > MaxWorkerId || _workerId < 0)
            throw new ArgumentException($"WorkerId必须�?-{MaxWorkerId}之间");
        if (_datacenterId > MaxDatacenterId || _datacenterId < 0)
            throw new ArgumentException($"DatacenterId必须�?-{MaxDatacenterId}之间");
    }

    public long NextId()
    {
        lock (_lock)
        {
            var timestamp = GetTimestamp();
            if (timestamp < _lastTimestamp)
                throw new InvalidOperationException("时钟回拨，拒绝生成ID");

            if (timestamp == _lastTimestamp)
            {
                _sequence = (_sequence + 1) & SequenceMask;
                if (_sequence == 0)
                    timestamp = WaitNextMillis(_lastTimestamp);
            }
            else
            {
                _sequence = 0L;
            }

            _lastTimestamp = timestamp;

            return ((timestamp - Twepoch) << (int)TimestampLeftShift)
                   | (_datacenterId << (int)DatacenterIdShift)
                   | (_workerId << (int)WorkerIdShift)
                   | _sequence;
        }
    }

    public string NextIdString() => NextId().ToString();

    private long GetTimestamp()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    private long WaitNextMillis(long lastTimestamp)
    {
        var timestamp = GetTimestamp();
        while (timestamp <= lastTimestamp)
        {
            Thread.Sleep(1);
            timestamp = GetTimestamp();
        }
        return timestamp;
    }
}