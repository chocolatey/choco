using Chocolatey.PowerShell.Helpers;
using Chocolatey.PowerShell.Shared;
using System;
using System.IO;
using System.Management.Automation;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static chocolatey.StringResources.EnvironmentVariables;

namespace Chocolatey.PowerShell.Commands
{
    [Cmdlet(VerbsData.Expand, "ChocolateyArchive", DefaultParameterSetName = "Path", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
    [OutputType(typeof(string))]
    public class ExpandChocolateyArchiveCommand : ChocolateyCmdlet
    {
        [Alias("File", "FileFullPath")]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Path")]
        [Parameter(Mandatory = true, ParameterSetName = "BothPaths")]
        public string Path { get; set; } = string.Empty;

        [Alias("UnzipLocation")]
        [Parameter(Mandatory = true, Position = 1)]
        public string Destination { get; set; } = string.Empty;

        [Parameter(Position = 2)]
        [Alias("SpecificFolder")]
        public string FilesToExtract { get; set; }

        [Parameter(Position = 3)]
        public string PackageName { get; set; }

        [Alias("File64", "FileFullPath64")]
        [Parameter(Mandatory = true, ParameterSetName = "Path64")]
        [Parameter(Mandatory = true, ParameterSetName = "BothPaths")]
        public string Path64 { get; set; }

        [Parameter]
        public SwitchParameter DisableLogging { get; set; }

        [Parameter]
        public SwitchParameter UseBuiltinCompression { get; set; } = EnvironmentHelper.GetVariable(Package.ChocolateyUseBuiltinCompression) == "true";

        protected override void End()
        {
            var helper = new ExtractArchiveHelper(this, PipelineStopToken);
            try
            {
                helper.ExtractFiles(Path, Path64, PackageName, Destination, FilesToExtract, UseBuiltinCompression, DisableLogging);

                WriteObject(Destination);
            }
            catch (FileNotFoundException error)
            {
                ThrowTerminatingError(new ErrorRecord(
                    error,
                    $"{ErrorId}.FileNotFound",
                    ErrorCategory.ObjectNotFound,
                    string.IsNullOrEmpty(Path) ? Path64 : Path));
            }
            catch (InvalidOperationException error)
            {
                ThrowTerminatingError(new ErrorRecord(
                    error,
                    $"{ErrorId}.ApplicationMissing",
                    ErrorCategory.InvalidOperation,
                    targetObject: null));
            }
            catch (NotSupportedException error)
            {
                ThrowTerminatingError(new ErrorRecord(
                    error,
                    $"{ErrorId}.UnsupportedArchitecture",
                    ErrorCategory.NotImplemented,
                    string.IsNullOrEmpty(Path) ? Path64 : Path));
            }
            catch (SevenZipException error)
            {
                ThrowTerminatingError(new ErrorRecord(
                    error,
                    $"{ErrorId}.ExtractionFailed",
                    ErrorCategory.InvalidResult,
                    string.IsNullOrEmpty(Path) ? Path64 : Path));
            }
            catch (Exception error)
            {
                ThrowTerminatingError(new ErrorRecord(
                    error,
                    $"{ErrorId}.Unknown",
                    ErrorCategory.NotSpecified,
                    string.IsNullOrEmpty(Path) ? Path64 : Path));
            }
        }
    }
}
