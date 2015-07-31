// Step 0. Boilerplate to get the paket.exe tool
 
open System
open System.IO
 
Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
 
if not (File.Exists "paket.exe") then
    let url = "https://github.com/fsprojects/Paket/releases/download/0.31.5/paket.exe"
    use wc = new Net.WebClient()
    let tmp = Path.GetTempFileName()
    wc.DownloadFile(url, tmp)
    File.Move(tmp,Path.GetFileName url)
 
// Step 1. Resolve and install the packages
 
#r "paket.exe"
 
Paket.Dependencies.Install """
source https://nuget.org/api/v2
nuget Suave
nuget FSharp.Data
nuget FSharp.Charting
""";;
 
// Step 2. Use the packages
 
#r "packages/Suave/lib/net40/Suave.dll"
#r "packages/FSharp.Data/lib/net40/FSharp.Data.dll"
#r "packages/FSharp.Charting/lib/net40/FSharp.Charting.dll"
  
open Suave // always open suave
open Suave.Http.Applicatives
open Suave.Http
open Suave.Http.Successful
open Suave.Web // for config
open Suave.Types

type canopyConfig = FSharp.Data.XmlProvider<"""<Config>
<CanopyPath>C:\canopy.exe</CanopyPath>
<CanopyArguments>one</CanopyArguments>
<UseShellExecute>true</UseShellExecute>	
</Config>""">

let loadedCanopyConfig =
    try 
        sprintf "%s\canopy.config" System.Environment.CurrentDirectory |> canopyConfig.Load
    with
        | ex ->        
            printf "Failed to load config at %s\canopy.config\n" <| System.Environment.CurrentDirectory 
            printf "Stacktrace: %s\n" ex.StackTrace
            canopyConfig.GetSample()

let startCanopy request =
    let processinfo = System.Diagnostics.ProcessStartInfo(loadedCanopyConfig.CanopyPath)
    processinfo.Arguments <- loadedCanopyConfig.CanopyArguments
    processinfo.UseShellExecute <- loadedCanopyConfig.UseShellExecute

    printf "Canopy path is %s\n" loadedCanopyConfig.CanopyPath
    printf "Creating canopy instance with the argument %s\n" loadedCanopyConfig.CanopyArguments

    let canopyProcess = System.Diagnostics.Process.Start(processinfo)
    canopyProcess.WaitForExit()
    OK <| sprintf "Finished. Canopy returned %i" canopyProcess.ExitCode

let app =
    choose [ GET >>= choose
        [ path "/" >>= request startCanopy ]
        ]

    
Suave.Web.startWebServer defaultConfig app
