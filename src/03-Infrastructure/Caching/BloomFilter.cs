using System.Collections;
using System.Security.Cryptography;
using System.Text;

namespace EfCore.Enterprise.Infrastructure.Caching;

public class BloomFilter
{
    private readonly BitArray _bits;
    private readonly int _hashFunctions;
    private readonly int _size;

    public BloomFilter(int expectedElements, double falsePositiveRate)
    {
        _size = (int)(-expectedElements * Math.Log(falsePositiveRate) / (Math.Log(2) * Math.Log(2)));
        _hashFunctions = (int)(_size / (double)expectedElements * Math.Log(2));
        _bits = new BitArray(_size);
    }

    public void Add(string item)
    {
        var hashes = GetHashes(item);
        foreach (var hash in hashes)
        {
            _bits.Set((int)(hash % _size), true);
        }
    }

    public bool Contains(string item)
    {
        var hashes = GetHashes(item);
        return hashes.All(hash => _bits[(int)(hash % _size)]);
    }

    private IEnumerable<int> GetHashes(string item)
    {
        var bytes = Encoding.UTF8.GetBytes(item);
        var md5Hash = MD5.HashData(bytes);
        var sha1Hash = SHA1.HashData(bytes);

        for (var i = 0; i < _hashFunctions; i++)
        {
            var combined = (long)BitConverter.ToInt32(md5Hash, i % 12) << 32
                         | (uint)BitConverter.ToInt32(sha1Hash, (i * 2) % 16);
            yield return Math.Abs(unchecked((int)combined));
        }
    }
}