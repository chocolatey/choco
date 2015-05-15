namespace chocolatey.infrastructure.adapters
{
    // ReSharper disable InconsistentNaming
    
    public interface IHashAlgorithm
    {
        byte[] ComputeHash(byte[] buffer);
    }

    // ReSharper restore InconsistentNaming
}