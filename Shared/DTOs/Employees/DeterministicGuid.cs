using System.Security.Cryptography;
using System.Text;

namespace Shared.DTOs.Employees;


public static class DeterministicGuid
{
    public static Guid CreateFromString(string input)
    {
        using MD5 md5 = MD5.Create();
        byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
        return new Guid(hash);
    }
}
