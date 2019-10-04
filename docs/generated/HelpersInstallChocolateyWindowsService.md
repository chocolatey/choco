# Install-ChocolateyWindowsService

**NOTE**: This function requires a Chocolatey for Business License to use.


Install-ChocolateyWindowsService [-Name] <string> [-ServiceExecutablePath] <string> [[-Username] <string>] [-Password <string>] [-DisplayName <string>] [-Description <string>] [-StartupType <ChocolateyWindowsServiceStartupType>] [-DoNotStartService] [-DoNotReinstallService]  [<CommonParameters>]


## Syntax

~~~powershell
Install-ChocolateyWindowsService `
  -Name <string> `
  -ServiceExecutablePath <string> `
  [-Username <string>] `
  [-Password <string>] `
  [-DisplayName <string>] `
  [-Description <string>] `
  [-StartupType <ChocolateyWindowsServiceStartupType> {Unknown | Manual | Automatic | Disabled}] `
  [-DoNotStartService] `
  [-DoNotReinstallService]
~~~



## Aliases

None

## Inputs

None

## Outputs

None

## Parameters

###  -Description [&lt;string&gt;]
The description of the service.


Property               | Value
---------------------- | ---------------------
Aliases                | 
Required?              | false
Position?              | Named
Default Value          | 
Accept Pipeline Input? | true (ByPropertyName)
 
###  -DisplayName [&lt;string&gt;]
The display name of the service.


Property               | Value
---------------------- | ---------------------
Aliases                | 
Required?              | false
Position?              | Named
Default Value          | 
Accept Pipeline Input? | true (ByPropertyName)
 
###  -DoNotReinstallService
Do not uninstall/restart service. This is for advanced scenarios when you need 
to deploy a newer version of a service and control when the restart happens over
to the newly deployed code.


Property               | Value
---------------------- | ---------------------
Aliases                | 
Required?              | false
Position?              | Named
Default Value          | 
Accept Pipeline Input? | true (ByPropertyName)
 
###  -DoNotStartService
Do not start service after install. This keeps the service from starting up when
installing/upgrading.


Property               | Value
---------------------- | ---------------------
Aliases                | 
Required?              | false
Position?              | Named
Default Value          | 
Accept Pipeline Input? | true (ByPropertyName)
 
###  -Name &lt;string&gt;
The name of the service to install.


Property               | Value
---------------------- | ---------------------
Aliases                | 
Required?              | true
Position?              | 0
Default Value          | 
Accept Pipeline Input? | true (ByPropertyName)
 
###  -Password [&lt;string&gt;]

The password for the service - defaults to empty. If the user is not a built-in 
account like LocalSystem and the user name is provided without a password being
provided, the password will automatically be a Chocolatey Managed Password.

When Chocolatey manages the password for an account, it creates a very complex 
password:

* 32 characters long
* Uppercase, lowercase, numbers, and symbols to meet very stringent complexity 
  requirements
* Different for every machine
* Completely unguessable

No one at Chocolatey Software could even tell you what the password is for a 
particular machine without local access.


Property               | Value
---------------------- | ---------------------
Aliases                | 
Required?              | false
Position?              | Named
Default Value          | 
Accept Pipeline Input? | true (ByPropertyName)
 
###  -ServiceExecutablePath &lt;string&gt;
The full path (absolute path) to the service executable file.


Property               | Value
---------------------- | ---------------------
Aliases                | 
Required?              | true
Position?              | 1
Default Value          | 
Accept Pipeline Input? | true (ByPropertyName)
 
###  -StartupType [&lt;ChocolateyWindowsServiceStartupType&gt;]
The startup type of the service. Defaults to Automatic.


Property               | Value
---------------------- | ---------------------
Aliases                | 
Required?              | false
Position?              | Named
Default Value          | 
Accept Pipeline Input? | true (ByPropertyName)
 
###  -Username [&lt;string&gt;]
The username for the service - defaults to LocalSystem (SYSTEM). If the user 
does not exist, the user will be created. When a user is specified for a 
service, the following things will also occur as part of this function:

* User added to Administrators group
* User given privilege/right to run as a service (SeServiceLogonRight)
* User given privilege/right to log on as a batch (SeBatchLogonRight)
* User given privilege/right to log on interactively (SeInteractiveLogonRight)
* User given privilege/right to log on network (SeNetworkLogonRight)



Property               | Value
---------------------- | ---------------------
Aliases                | user
Required?              | false
Position?              | 2
Default Value          | 
Accept Pipeline Input? | true (ByPropertyName)
 



[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from licensed code.
