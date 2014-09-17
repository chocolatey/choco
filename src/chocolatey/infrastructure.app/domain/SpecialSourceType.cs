namespace chocolatey.infrastructure.app.domain
{
    /// <summary>
    ///   Special source modifiers that use alternate sources for packages
    /// </summary>
    public enum SpecialSourceType
    {
        //this is what it should be when it's not set
        normal,
        webpi,
        ruby,
        python,
        windowsfeatures,
        cygwin,
    }
}