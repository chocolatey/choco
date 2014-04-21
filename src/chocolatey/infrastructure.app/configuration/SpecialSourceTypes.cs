namespace chocolatey.infrastructure.app.configuration
{
    /// <summary>
    /// Special source modifiers that use alternate sources for packages
    /// </summary>
    public enum SpecialSourceTypes
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