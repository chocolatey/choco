namespace chocolatey.infrastructure.app.domain
{
    public enum InstallerType
    {
        Unknown,
        Custom,
        Msi, 
        Nsis,
        InnoSetup,
        InstallShield,
        Zip,
        SevenZip,
        HotfixOrSecurityUpdate,
        ServicePack
    }
}