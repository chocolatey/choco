namespace chocolatey.infrastructure.adapters
{
    using System.IO;
    using cryptography;

    public sealed class HashAlgorithm : IHashAlgorithm
    {
        private readonly System.Security.Cryptography.HashAlgorithm _algorithm;

        public HashAlgorithm(System.Security.Cryptography.HashAlgorithm algorithm)
        {
            _algorithm = algorithm;
        }

        public byte[] ComputeHash(byte[] buffer)
        {
            return _algorithm.ComputeHash(buffer);
        }

        public byte[] ComputeHash(Stream stream)
        {
            return _algorithm.ComputeHash(stream);
        }
    }
}