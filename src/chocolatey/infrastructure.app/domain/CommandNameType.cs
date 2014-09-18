namespace chocolatey.infrastructure.app.domain
{
    using System.ComponentModel;

    public enum CommandNameType
    {
        [Description("list - lists remote or local packages")] list,
        [Description("search - searches remote or local packages")] search,
        [Description("install - installs packages from various sources")] install,
        //[Description("update - updates package index")]
        //update,
        [Description("upgrade - upgrades packages from various sources")] upgrade,
        [Description("uninstall - uninstalls a package")] uninstall,
        [Description("sources - view and configure default sources")] sources,
        [Description("config - view and change configuration")] config,
        [Description("unpackself - have chocolatey set it self up")] unpackself,
        [Description("pack - packages up a nuspec to a compiled nupkg")] pack,
        [Description("push - pushes a compiled nupkg")] push,
        [Description("new - generates files necessary for a chocolatey package")] @new,

    }
}