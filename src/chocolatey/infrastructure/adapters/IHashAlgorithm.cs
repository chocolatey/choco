namespace chocolatey.infrastructure.adapters
{
    using System.IO;
    // ReSharper disable InconsistentNaming
    
    public interface IHashAlgorithm
    {
        byte[] ComputeHash(byte[] buffer);
        
        byte[] ComputeHash(Stream stream);
    }

    // ReSharper restore InconsistentNaming
}