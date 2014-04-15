## Get-FtpFile
##############################################################################################################
## Downloads a file from ftp
## Some code from http://stackoverflow.com/questions/265339/whats-the-best-way-to-automate-secure-ftp-in-powershell
## Additional functionality emulated from http://poshcode.org/417 (Get-WebFile)
## Written by Stephen C. Austin, Pwnt & Co. http://pwnt.co
##############################################################################################################
function Get-FtpFile {
param(
  $url = '', #(Read-Host "The URL to download"),
  $fileName = $null,
  $username = $null,
  $password = $null,
  [switch]$quiet
)
  # Create a FTPWebRequest object to handle the connection to the ftp server
  $ftprequest = [System.Net.FtpWebRequest]::create($url)

  # set the request's network credentials for an authenticated connection
  $ftprequest.Credentials =
    New-Object System.Net.NetworkCredential($username,$password)

  $ftprequest.Method = [System.Net.WebRequestMethods+Ftp]::DownloadFile
  $ftprequest.UseBinary = $true
  $ftprequest.KeepAlive = $false

  # send the ftp request to the server
  $ftpresponse = $ftprequest.GetResponse()
  [int]$goal = $ftpresponse.ContentLength

  # get a download stream from the server response
  $reader = $ftpresponse.GetResponseStream()

  # create the target file on the local system and the download buffer
  $writer = New-Object IO.FileStream ($fileName,[IO.FileMode]::Create)
  [byte[]]$buffer = New-Object byte[] 1024
    [int]$total = [int]$count = 0

  # loop through the download stream and send the data to the target file
  do{
    $count = $reader.Read($buffer, 0, $buffer.Length);
    $writer.Write($buffer, 0, $count);
    if(!$quiet) {
      $total += $count
      if($goal -gt 0) {
        Write-Progress "Downloading $url to $fileName" "Saving $total of $goal" -id 0 -percentComplete (($total/$goal)*100)
      } else {
        Write-Progress "Downloading $url to $fileName" "Saving $total bytes..." -id 0 -Completed
      }
      if ($total -eq $goal) {
        Write-Progress "Completed download of $url." "Completed a total of $total bytes of $fileName" -id 0 -Completed
      }
    }
  } while ($count -ne 0)

  $writer.Flush()
  $writer.close()
}
