## Chocolatey Usage Scenarios

### ChocolateyInstallCommand [ 35 Scenario(s), 293 Observation(s) ]

#### when force installing a package that depends on an unavailable newer version of an installed dependency forcing dependencies

 * should contain a warning message that it was unable to install any packages
 * should have an error package result
 * should have expected error in package result
 * should not have a successful package result
 * should not have inconclusive package result
 * should not have warning package result
 * should not install a package in the lib directory
 * should not upgrade the dependency

#### when force installing an already installed package

 * config should match package result name
 * should contain a warning message that it installed successfully
 * should delete the rollback
 * should have a successful package result
 * should have a version of one dot zero dot zero
 * should install the package in the lib directory
 * should install the same version of the package
 * should install where install location reports
 * should not have inconclusive package result
 * should not have warning package result
 * should remove and re add the package files in the lib directory

#### when force installing an already installed package forcing and ignoring dependencies

 * should contain a warning message that it installed successfully
 * should have a successful package result
 * should install a package in the lib directory
 * should install where install location reports
 * should not have inconclusive package result
 * should not have warning package result
 * should reinstall the exact same version of the package
 * should remove the exact dependency
 * should remove the floating dependency

#### when force installing an already installed package forcing dependencies

 * should contain a warning message that it installed successfully
 * should have a successful package result
 * should install a package in the lib directory
 * should install the dependency in the lib directory
 * should install where install location reports
 * should not have inconclusive package result
 * should not have warning package result
 * should reinstall the exact same version of the exact dependency
 * should reinstall the exact same version of the package
 * should reinstall the floating dependency with the latest version that satisfies the dependency

#### when force installing an already installed package ignoring dependencies

 * should contain a warning message that it installed successfully
 * should have a successful package result
 * should install a package in the lib directory
 * should install the dependency in the lib directory
 * should install where install location reports
 * should not have inconclusive package result
 * should not have warning package result
 * should not touch the exact dependency
 * should not touch the floating dependency
 * should reinstall the exact same version of the package

#### when force installing an already installed package that errors

 * should contain a message that it was unsuccessful
 * should delete the rollback
 * should not have a successful package result
 * should not have inconclusive package result
 * should not have warning package result
 * should restore the backup version of the package
 * should restore the original files in the package lib folder

#### when force installing an already installed package with a read and delete share locked file

 * config should match package result name
 * should contain a message that it installed successfully
 * should have a successful package result
 * should have a version of one dot zero dot zero
 * should install where install location reports
 * should not be able delete the rollback
 * should not have inconclusive package result
 * should not have warning package result
 * should reinstall the package in the lib directory
 * should reinstall the same version of the package

#### when force installing an already installed package with dependencies

 * should contain a message that it installed successfully
 * should have a successful package result
 * should have a version of one dot zero dot zero
 * should install a package in the lib directory
 * should install where install location reports
 * should not have inconclusive package result
 * should not have warning package result
 * should not upgrade the dependency
 * should reinstall the exact same version of the package
 * should still have the dependency in the lib directory

#### when force installing an already installed package with with an exclusively locked file

 * [PENDING] should delete the rollback
 * [PENDING] should not have a successful package result
 * [PENDING] should not have warning package result
 * should contain a message that it was unable to reinstall successfully
 * should have a package installed in the lib directory
 * should have inconclusive package result
 * should still have the package installed with the expected version of the package

#### when installing a package from a nupkg file

 * config should match package result name
 * should contain a warning message that it installed successfully
 * should create a shim for console in the bin directory
 * should create a shim for graphical in the bin directory
 * should have a console shim that is set for non gui access
 * should have a graphical shim that is set for gui access
 * should have a successful package result
 * should have a version of one dot zero dot zero
 * should install the expected version of the package
 * should install the package in the lib directory
 * should install where install location reports
 * should not create a shim for ignored executable in the bin directory
 * should not create a shim for mismatched case ignored executable in the bin directory
 * should not create an extensions folder for the package
 * should not have inconclusive package result
 * should not have warning package result

#### when installing a package happy path

 * config should match package result name
 * should contain a warning message that it installed successfully
 * should create a shim for console in the bin directory
 * should create a shim for graphical in the bin directory
 * should have a console shim that is set for non gui access
 * should have a graphical shim that is set for gui access
 * should have a successful package result
 * should have a version of one dot zero dot zero
 * should have executed chocolateyInstall script
 * should install the expected version of the package
 * should install the package in the lib directory
 * should install where install location reports
 * should not create a shim for ignored executable in the bin directory
 * should not create a shim for mismatched case ignored executable in the bin directory
 * should not create an extensions folder for the package
 * should not have inconclusive package result
 * should not have warning package result

#### when installing a package ignoring dependencies that cannot be found

 * config should match package result name
 * should contain a warning message that it installed successfully
 * should have a successful package result
 * should install a package in the lib directory
 * should install the expected version of the package
 * should install where install location reports
 * should not have inconclusive package result
 * should not have warning package result
 * should not install the dependency in the lib directory

#### when installing a package that depends on a newer version of an installed dependency

 * should contain a warning message that it installed successfully
 * should have a successful package result
 * should install a package in the lib directory
 * should install the dependency in the lib directory
 * should install the expected version of the package
 * should install where install location reports
 * should not have inconclusive package result
 * should not have warning package result
 * should upgrade the dependency

#### when installing a package that depends on an unavailable newer version of an installed dependency

 * should contain a message that is was unable to install any packages
 * should not have a successful package result
 * should not have inconclusive package result
 * should not have warning package result
 * should not install the package in the lib directory

#### when installing a package that depends on an unavailable newer version of an installed dependency ignoring dependencies

 * should contain a message that it installed successfully
 * should have a successful package result
 * should install a package in the lib directory
 * should install the expected version of the package
 * should install where install location reports
 * should not have inconclusive package result
 * should not have warning package result

#### when installing a package that does not exist

 * should contain a warning message that it was unable to install a package
 * should have an error package result
 * should have expected error in package result
 * should not have a successful package result
 * should not have inconclusive package result
 * should not have warning package result
 * should not install a package in the lib directory

#### when installing a package that errors

 * should contain a warning message that it was unable to install a package
 * should have an error package result
 * should have expected error in package result
 * should not have a successful package result
 * should not have inconclusive package result
 * should not have warning package result
 * should not install a package in the lib directory
 * should put a package in the lib bad directory

#### when installing a package that exists but a version that does not exist

 * should contain a warning message that it did not install successfully
 * should have a version of one dot zero dot one
 * should have an error package result
 * should have expected error in package result
 * should not have a successful package result
 * should not have inconclusive package result
 * should not have warning package result
 * should not install a package in the lib directory

#### when installing a package that has nonterminating errors

 * config should match package result name
 * should contain a message that it installed successfully
 * should have a successful package result
 * should have a version of one dot zero
 * should install the expected version of the package
 * should install the package in the lib directory
 * should install where install location reports
 * should not have inconclusive package result
 * should not have warning package result

#### when installing a package that has nonterminating errors with fail on stderr

 * should contain a warning message that it was unable to install a package
 * should have an error package result
 * should have expected error in package result
 * should not have a successful package result
 * should not have inconclusive package result
 * should not have warning package result
 * should not install a package in the lib directory
 * should put a package in the lib bad directory

#### when installing a package with a dependent package that also depends on a less constrained but still valid dependency of the same package

 * [PENDING] should contain a message that everything installed successfully
 * [PENDING] should have a successful package result
 * [PENDING] should install a package in the lib directory
 * [PENDING] should install the dependency in the lib directory
 * [PENDING] should install the expected version of the constrained dependency
 * [PENDING] should install the expected version of the dependency
 * [PENDING] should install where install location reports
 * [PENDING] should not have inconclusive package result
 * [PENDING] should not have warning package result

#### when installing a package with config transforms

 * should add the insert value in the config due to XDT InsertIfMissing
 * should change the testReplace value in the config due to XDT Replace
 * should contain a warning message that it installed successfully
 * should have a successful package result
 * should have a version of one dot zero dot zero
 * should install the expected version of the package
 * should not change the test value in the config due to XDT InsertIfMissing
 * should not have inconclusive package result
 * should not have warning package result

#### when installing a package with dependencies and dependency cannot be found

 * should contain a warning message that it was unable to install any packages
 * should have an error package result
 * should have expected error in package result
 * should not have a successful package result
 * should not have inconclusive package result
 * should not have warning package result
 * should not install a package in the lib directory
 * should not install the dependency in the lib directory

#### when installing a package with dependencies happy

 * should contain a message that everything installed successfully
 * should have a successful package result
 * should have a version of one dot zero dot zero
 * should install a package in the lib directory
 * should install the dependency in the lib directory
 * should install the expected version of the dependency
 * should install where install location reports
 * should not have inconclusive package result
 * should not have warning package result

#### when installing a package with dependencies on a newer version of a package than an existing package has with that dependency

 * should contain a message that it installed successfully
 * should have a successful package result
 * should install a package in the lib directory
 * should install where install location reports
 * should not have inconclusive package result
 * should not have warning package result
 * should upgrade the dependency

#### when installing a package with dependencies on a newer version of a package than are allowed by an existing package with that dependency

 * [PENDING] should contain a message that it was unable to install any packages
 * [PENDING] should have an error package result
 * [PENDING] should not have a successful package result
 * [PENDING] should not have inconclusive package result
 * [PENDING] should not have warning package result
 * [PENDING] should not install the conflicting package
 * [PENDING] should not install the conflicting package in the lib directory
 * [PENDING] should not upgrade the exact version dependency
 * [PENDING] should not upgrade the minimum version dependency

#### when installing a package with dependencies on an older version of a package than is already installed

 * [PENDING] should contain a message that it was unable to install any packages
 * [PENDING] should have an error package result
 * [PENDING] should not have a successful package result
 * [PENDING] should not have inconclusive package result
 * [PENDING] should not have warning package result
 * [PENDING] should not install the conflicting package in the lib directory
 * [PENDING] should not upgrade the exact version dependency

#### when installing a package with no sources enabled

 * should have no sources enabled result
 * should not install any packages

#### when installing a side by side package

 * config should match package result name
 * should contain a warning message that it installed successfully
 * should have a successful package result
 * should have a version of one dot zero dot zero
 * should install a package in the lib directory
 * should install where install location reports
 * should not have inconclusive package result
 * should not have warning package result

#### when installing an already installed package

 * should ave warning package result
 * should contain a message about force to reinstall
 * should contain a warning message that it was unable to install any packages
 * should have inconclusive package result
 * should still have a package in the lib directory
 * should still have the expected version of the package installed

#### when installing packages with packages config

 * should contain a message that upgradepackage with an expected specified version was installed
 * should contain a warning message that it installed 4 out of 5 packages successfully
 * should have a successful package result for all but expected missing package
 * should install expected packages in the lib directory
 * should install the dependency in the lib directory
 * should install where install location reports
 * should not have a successful package result for missing package
 * should not have inconclusive package result
 * should not have warning package result
 * should print out package from config file in message
 * should specify config file is being used in message

#### when noop installing a package

 * should contain a message that it would have run a powershell script
 * should contain a message that it would have used Nuget to install a package
 * should not contain a message that it would have run powershell modification script
 * should not install a package in the lib directory

#### when noop installing a package that does not exist

 * should contain a message that it was unable to find package
 * should contain a message that it would have used Nuget to install a package
 * should not install a package in the lib directory

#### when switching a normal package to a side by side package

 * config should match package result name
 * should contain a warning message that it installed successfully
 * should have a successful package result
 * should have a version of one dot zero dot zero
 * should install a package in the lib directory
 * should install where install location reports
 * should not have inconclusive package result
 * should not have warning package result

#### when switching a side by side package to a normal package

 * config should match package result name
 * should contain a warning message that it installed successfully
 * should have a successful package result
 * should have a version of one dot zero dot zero
 * should install a package in the lib directory
 * should install where install location reports
 * should not have inconclusive package result
 * should not have warning package result

### ChocolateyListCommand [ 10 Scenario(s), 41 Observation(s) ]

#### when listing local packages

 * should contain a summary
 * should contain debugging messages
 * should contain packages and versions with a space between them
 * should not contain packages and versions with a pipe between them

#### when listing local packages limiting output

 * should contain packages and versions with a pipe between them
 * should not contain a summary
 * should not contain debugging messages
 * should not contain packages and versions with a space between them
 * should only have messages related to package information

#### when listing local packages limiting output with id only

 * should contain packages id
 * should not contain any version number
 * should not contain pipe

#### when listing local packages with id only

 * should contain package name
 * should not contain any version number

#### when listing packages with no sources enabled

 * should have no sources enabled result
 * should not list any packages

#### when searching all available packages

 * should contain a summary
 * should contain debugging messages
 * should contain packages and versions with a space between them
 * should list available packages as many times as they show on the feed
 * should not contain packages and versions with a pipe between them

#### when searching for a particular package

 * should contain a summary
 * should contain debugging messages
 * should contain packages and versions with a space between them
 * should not contain packages that do not match

#### when searching for an exact package

 * should contain a summary
 * should contain debugging messages
 * should contain packages and versions with a space between them
 * should not contain packages that do not match

#### when searching packages with no filter happy path

 * should contain a summary
 * should contain debugging messages
 * should contain packages and versions with a space between them
 * should list available packages only once
 * should not contain packages and versions with a pipe between them

#### when searching packages with verbose

 * should contain a summary
 * should contain debugging messages
 * should contain description
 * should contain download counts
 * should contain packages and versions with a space between them
 * should contain tags
 * should not contain packages and versions with a pipe between them

### ChocolateyPackCommand [ 3 Scenario(s), 6 Observation(s) ]

#### when packing with an output directory

 * generated package should be in specified output directory
 * sources should be set to specified output directory

#### when packing with properties

 * generated package should be in current directory
 * property settings should be logged as debug messages

#### when packing without specifying an output directory

 * generated package should be in current directory
 * sources should be set to current directory

### ChocolateyPinCommand [ 9 Scenario(s), 12 Observation(s) ]

#### when listing pins with an existing pin

 * should contain existing pin messages
 * should not contain list results

#### when listing pins with existing pins

 * should contain a pin message for each existing pin
 * should not contain list results

#### when listing pins with no pins

 * should not contain any pins by default
 * should not contain list results

#### when removing a pin for a non installed package

 * should throw an error about not finding the package

#### when removing a pin for a pinned package

 * should contain success message

#### when removing a pin for an unpinned package

 * should contain nothing to do message

#### when setting a pin for a non installed package

 * should throw an error about not finding the package

#### when setting a pin for an already pinned package

 * should contain nothing to do message

#### when setting a pin for an installed package

 * should contain success message

### ChocolateyUninstallCommand [ 13 Scenario(s), 93 Observation(s) ]

#### when force uninstalling a package

 * config should match package result name
 * should contain a warning message that it uninstalled successfully
 * should delete a shim for console in the bin directory
 * should delete a shim for graphical in the bin directory
 * should delete the rollback
 * should have a successful package result
 * should not have inconclusive package result
 * should not have warning package result
 * should remove the package from the lib directory

#### when force uninstalling a package with added and changed files

 * config should match package result name
 * should contain a warning message that it uninstalled successfully
 * should delete a shim for console in the bin directory
 * should delete a shim for graphical in the bin directory
 * should delete the rollback
 * should have a successful package result
 * should not have inconclusive package result
 * should not have warning package result
 * should not keep the added file
 * should not keep the changed file
 * should remove the package from the lib directory

#### when noop uninstalling a package

 * should contain a message that it would have run a powershell script
 * should contain a message that it would have run powershell modification script
 * should contain a message that it would have uninstalled a package
 * should not uninstall a package from the lib directory

#### when noop uninstalling a package that does not exist

 * should contain a message that it was unable to find package

#### when uninstalling a package happy path

 * config should match package result name
 * should contain a warning message that it uninstalled successfully
 * should delete a shim for console in the bin directory
 * should delete a shim for graphical in the bin directory
 * should delete any files created during the install
 * should delete the rollback
 * should have a successful package result
 * should have executed chocolateyBeforeModify script
 * should have executed chocolateyUninstall script
 * should not have inconclusive package result
 * should not have warning package result
 * should remove the package from the lib directory

#### when uninstalling a package that does not exist

 * should contain a message that it was unable to find package
 * should contain a warning message that it uninstalled successfully
 * should have an error package result
 * should not have a successful package result
 * should not have inconclusive package result
 * should not have warning package result

#### when uninstalling a package that errors

 * should contain a warning message that it was unable to install a package
 * should have an error package result
 * should have expected error in package result
 * should not delete the rollback
 * should not have a successful package result
 * should not have inconclusive package result
 * should not have warning package result
 * should not put the package in the lib bad directory
 * should not remove package from the lib directory
 * should still have the package file in the directory

#### when uninstalling a package with a read and delete share locked file

 * should contain a message that it uninstalled successfully
 * should have a successful package result
 * should not be able delete the rollback
 * should not have inconclusive package result
 * should not have warning package result
 * should uninstall the package from the lib directory

#### when uninstalling a package with added files

 * config should match package result name
 * should contain a warning message that it uninstalled successfully
 * should delete a shim for console in the bin directory
 * should delete a shim for graphical in the bin directory
 * should delete everything but the added file from the package directory
 * should delete the rollback
 * should have a successful package result
 * should keep the added file
 * should not have inconclusive package result
 * should not have warning package result

#### when uninstalling a package with an exclusively locked file

 * should contain a message that it was not able to uninstall
 * should contain old files in directory
 * should delete the rollback
 * should not be able to remove the package from the lib directory
 * should not have a successful package result
 * should not have inconclusive package result
 * should not have warning package result

#### when uninstalling a package with changed files

 * config should match package result name
 * should contain a warning message that it uninstalled successfully
 * should delete a shim for console in the bin directory
 * should delete a shim for graphical in the bin directory
 * should delete everything but the changed file from the package directory
 * should delete the rollback
 * should have a successful package result
 * should keep the changed file
 * should not have inconclusive package result
 * should not have warning package result

#### when uninstalling a package with readonly files

 * should contain a message that it uninstalled successfully
 * should delete the rollback
 * should have a successful package result
 * should not have inconclusive package result
 * should not have warning package result
 * should uninstall the package from the lib directory

#### when uninstalling packages with packages config

 * should throw an error that it is not allowed

### ChocolateyUpgradeCommand [ 36 Scenario(s), 295 Observation(s) ]

#### when force upgrading a package

 * config should match package result name
 * should contain a warning message that it upgraded successfully
 * should contain a warning message with old and new versions
 * should contain newer version in directory
 * should delete the rollback
 * should have a successful package result
 * should match the upgrade version of one dot one dot zero
 * should not have inconclusive package result
 * should not have warning package result
 * should upgrade a package in the lib directory
 * should upgrade the package
 * should upgrade where install location reports

#### when force upgrading a package that does not have available upgrades

 * should be the same version of the package
 * should contain a message that the package was upgraded
 * should contain a message that you have the latest version available
 * should have a successful package result
 * should match the existing version of one dot zero dot zero
 * should not create a rollback
 * should not have inconclusive package result
 * should not have warning package result
 * should not remove the package from the lib directory

#### when noop upgrading a package that does not exist

 * should contain a message that no packages can be upgraded
 * should contain a message the package was not found

#### when noop upgrading a package that does not have available upgrades

 * should contain a message that no packages can be upgraded
 * should contain a message that you have the latest version available
 * should not create a rollback

#### when noop upgrading a package that has available upgrades

 * should contain a message that a new version is available
 * should contain a message that a package can be upgraded
 * should contain older version in directory
 * should not create a rollback

#### when upgrading a dependency happy

 * should contain a message the dependency upgraded successfully
 * should have a successful package result
 * should not have inconclusive package result
 * should not have warning package result
 * should not upgrade the exact version dependency
 * should not upgrade the parent package
 * should upgrade the package

#### when upgrading a dependency legacy folder version

 * should contain a message the dependency upgraded successfully
 * should have a successful package result
 * should leave the exact version package as legacy folder
 * should leave the parent package as legacy folder
 * should not add a versionless exact version package folder to the lib dir
 * should not add a versionless parent package folder to the lib dir
 * should not have inconclusive package result
 * should not have warning package result
 * should not upgrade the exact version dependency
 * should not upgrade the parent package
 * should remove the legacy folder version of the package
 * should replace the legacy folder version of the package with a lib package folder that has no version
 * should upgrade the package

#### when upgrading a dependency with parent that depends on a range less than upgrade version

 * should contain a message that everything upgraded successfully
 * should have a successful package result
 * should not have inconclusive package result
 * should not have warning package result
 * should upgrade the exact version dependency
 * should upgrade the package
 * should upgrade the parent package to lowest version that meets new dependency version

#### when upgrading a legacy folder dependency with parent that depends on a range less than upgrade version

 * [PENDING] should remove the legacy folder version of the exact version package
 * [PENDING] should remove the legacy folder version of the parent package
 * should contain a message that everything upgraded successfully
 * should have a successful package result
 * should not have inconclusive package result
 * should not have warning package result
 * should remove the legacy folder version of the package
 * should replace the legacy folder version of the exact version package with a lib package folder that has no version
 * should replace the legacy folder version of the package with a lib package folder that has no version
 * should replace the legacy folder version of the parent package with a lib package folder that has no version
 * should upgrade the exact version dependency
 * should upgrade the package
 * should upgrade the parent package to lowest version that meets new dependency version

#### when upgrading a package that does not exist

 * should contain a message that no packages were upgraded
 * should contain a message the package was not found
 * should have an error package result
 * should have expected error in package result
 * should not have a successful package result
 * should not have inconclusive package result
 * should not have warning package result
 * should not install a package in the lib directory

#### when upgrading a package that does not have available upgrades

 * should be the same version of the package
 * should contain a message that no packages were upgraded
 * should contain a message that you have the latest version available
 * should have a successful package result
 * should have inconclusive package result
 * should match the existing version of one dot zero dot zero
 * should not create a rollback
 * should not have warning package result
 * should not remove the package from the lib directory

#### when upgrading a package that errors

 * should contain a warning message that it was unable to upgrade a package
 * should delete the rollback
 * should have an error package result
 * should have expected error in package result
 * should have the erroring upgraded package in the lib bad directory
 * should not have a successful package result
 * should not have inconclusive package result
 * should not have warning package result
 * should not remove package from the lib directory
 * should not upgrade the package
 * should put the package in the lib bad directory

#### when upgrading a package that is not installed

 * should contain a warning message that it upgraded successfully
 * should have a successful package result
 * should install a package in the lib directory
 * should install where install location reports
 * should not have a rollback directory
 * should not have inconclusive package result
 * should not have warning package result

#### when upgrading a package that is not installed and failing on not installed

 * should contain a warning message that it was unable to upgrade a package
 * should have an error package result
 * should have expected error in package result
 * should not have a successful package result
 * should not have inconclusive package result
 * should not have warning package result
 * should not install a package in the lib directory

#### when upgrading a package with a read and delete share locked file

 * should contain a warning message that it upgraded successfully
 * should contain a warning message with old and new versions
 * should contain newer version in directory
 * should have a successful package result
 * should not be able delete the rollback
 * should not have inconclusive package result
 * should not have warning package result
 * should upgrade a package in the lib directory
 * should upgrade the package
 * should upgrade where install location reports

#### when upgrading a package with added files

 * should contain a warning message that it upgraded successfully
 * should contain newer version in directory
 * should have a successful package result
 * should keep the added file
 * should match the upgrade version of one dot one dot zero
 * should not have inconclusive package result
 * should not have warning package result
 * should upgrade the package

#### when upgrading a package with an exclusively locked file

 * should contain a warning message that it was not able to upgrade
 * should contain a warning message with old and new versions
 * should contain old version in directory
 * should delete the rollback
 * should have a package installed in the lib directory
 * should not have a successful package result
 * should not have inconclusive package result
 * should not have warning package result
 * should not upgrade the package

#### when upgrading a package with changed files

 * should contain a warning message that it upgraded successfully
 * should contain newer version in directory
 * should have a successful package result
 * should match the upgrade version of one dot one dot zero
 * should not have inconclusive package result
 * should not have warning package result
 * should update the changed file
 * should upgrade the package

#### when upgrading a package with config transforms

 * should add the insertNew value in the config due to XDT InsertIfMissing
 * should change the testReplace value in the config due to XDT Replace
 * should contain a warning message that it upgraded successfully
 * should have a successful package result
 * should match the upgrade version of one dot one dot zero
 * should not change the insert value in the config due to upgrade and XDT InsertIfMissing
 * should not change the test value in the config from original one dot zero dot zero due to upgrade and XDT InsertIfMissing
 * should not have inconclusive package result
 * should not have warning package result
 * should upgrade the package

#### when upgrading a package with config transforms when config was edited

 * should add the insertNew value in the config due to XDT InsertIfMissing
 * should change the testReplace value in the config due to XDT Replace
 * should contain a warning message that it upgraded successfully
 * should have a config with the comment from the original
 * should have a successful package result
 * should match the upgrade version of one dot one dot zero
 * should not change the insert value in the config due to upgrade and XDT InsertIfMissing
 * should not change the test value in the config from original one dot zero dot zero due to upgrade and XDT InsertIfMissing
 * should not have inconclusive package result
 * should not have warning package result
 * should upgrade the package

#### when upgrading a package with dependencies happy

 * should contain a message that everything upgraded successfully
 * should have a successful package result
 * should not have inconclusive package result
 * should not have warning package result
 * should upgrade the exact version dependency
 * should upgrade the minimum version dependency
 * should upgrade the package

#### when upgrading a package with no sources enabled

 * should have no sources enabled result
 * should not have any packages upgraded

#### when upgrading a package with readonly files

 * should contain a warning message that it upgraded successfully
 * should contain a warning message with old and new versions
 * should contain newer version in directory
 * should delete the rollback
 * should have a successful package result
 * should not have inconclusive package result
 * should not have warning package result
 * should upgrade a package in the lib directory
 * should upgrade the package
 * should upgrade where install location reports

#### when upgrading a package with unavailable dependencies

 * should contain a message that it was unable to upgrade anything
 * should have an error package result
 * should have expected error in package result
 * should not have a successful package result
 * should not have inconclusive package result
 * should not have warning package result
 * should not upgrade the exact version dependency
 * should not upgrade the minimum version dependency
 * should not upgrade the package

#### when upgrading a package with unavailable dependencies ignoring dependencies

 * should contain a message that it upgraded only the package successfully
 * should have a successful package result
 * should not have inconclusive package result
 * should not have warning package result
 * should not upgrade the exact version dependency
 * should not upgrade the minimum version dependency
 * should upgrade the package

#### when upgrading all packages happy path

 * should report for all installed packages
 * should skip packages without upgrades
 * should upgrade packages with upgrades

#### when upgrading all packages with except

 * should report for all non skipped packages
 * should skip packages in except list

#### when upgrading all packages with prereleases installed

 * should report for all installed packages
 * should skip packages without upgrades
 * should upgrade packages with upgrades
 * should upgrade upgradepackage

#### when upgrading all packages with prereleases installed with excludeprerelease specified

 * should not upgrade upgradepackage
 * should report for all installed packages
 * should skip packages without upgrades
 * should upgrade packages with upgrades

#### when upgrading an existing package happy path

 * config should match package result name
 * should contain a warning message that it upgraded successfully
 * should contain a warning message with old and new versions
 * should contain newer version in directory
 * should delete the rollback
 * should have a successful package result
 * should have executed chocolateyBeforeModify before chocolateyInstall
 * should have executed chocolateyBeforeModify script for original package
 * should have executed chocolateyInstall script for new package
 * should match the upgrade version of one dot one dot zero
 * should not have executed chocolateyBeforeModify script for new package
 * should not have executed chocolateyUninstall script for original package
 * should not have inconclusive package result
 * should not have warning package result
 * should upgrade a package in the lib directory
 * should upgrade the package
 * should upgrade where install location reports

#### when upgrading an existing package with prerelease available and prerelease specified

 * config should match package result name
 * should contain a warning message that it upgraded successfully
 * should contain a warning message with old and new versions
 * should contain newer version in directory
 * should delete the rollback
 * should have a successful package result
 * should have executed chocolateyBeforeModify before chocolateyInstall
 * should have executed chocolateyBeforeModify script for original package
 * should have executed chocolateyInstall script for new package
 * should match the upgrade version of the new beta
 * should not have executed chocolateyBeforeModify script for new package
 * should not have executed chocolateyUninstall script for original package
 * should not have inconclusive package result
 * should not have warning package result
 * should upgrade a package in the lib directory
 * should upgrade the package
 * should upgrade where install location reports

#### when upgrading an existing package with prerelease available without prerelease specified

 * should be the same version of the package
 * should contain a message that no packages were upgraded
 * should contain a message that you have the latest version available
 * should have a successful package result
 * should have inconclusive package result
 * should match the original package version
 * should not create a rollback
 * should not have warning package result
 * should not remove the package from the lib directory

#### when upgrading an existing prerelease package with allow downgrade with excludeprelease and without prerelease specified

 * should be the same version of the package
 * should contain a message that no packages were upgraded
 * should contain a message that you have the latest version available
 * should have a successful package result
 * should have inconclusive package result
 * should not create a rollback
 * should not have warning package result
 * should not remove the package from the lib directory
 * should only find the last stable version

#### when upgrading an existing prerelease package with prerelease available with excludeprelease and without prerelease specified

 * should be the same version of the package
 * should contain a message that no packages were upgraded
 * should contain a message that you have the latest version available
 * should have a successful package result
 * should have inconclusive package result
 * should not create a rollback
 * should not have warning package result
 * should not remove the package from the lib directory
 * should only find the last stable version

#### when upgrading an existing prerelease package without prerelease specified

 * config should match package result name
 * should contain a warning message that it upgraded successfully
 * should contain a warning message with old and new versions
 * should contain newer version in directory
 * should delete the rollback
 * should have a successful package result
 * should have executed chocolateyBeforeModify before chocolateyInstall
 * should have executed chocolateyBeforeModify script for original package
 * should have executed chocolateyInstall script for new package
 * should match the upgrade version of the new beta
 * should not have executed chocolateyBeforeModify script for new package
 * should not have executed chocolateyUninstall script for original package
 * should not have inconclusive package result
 * should not have warning package result
 * should upgrade a package in the lib directory
 * should upgrade the package
 * should upgrade where install location reports

#### when upgrading packages with packages config

 * should throw an error that it is not allowed
