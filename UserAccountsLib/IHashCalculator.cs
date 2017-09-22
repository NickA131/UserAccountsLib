using System;
namespace UserAccountsLib
{
    public interface IHashCalculator
    {
        string CreateHash(string text);
    }
}
