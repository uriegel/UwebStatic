open Session
open Request

System.IO.Directory.SetCurrentDirectory "/media/speicher/projekte/UwebServer"

printfn "Starting Test Server"

let reitbeteiligungRequest = 
    Static.useStatic "/media/speicher/projekte/UwebServer/webroot" "/Reitbeteiligung" 

let testRequest = 
    Static.useStatic "/media/speicher/projekte/UwebServer/webroot" "/test" 

let configuration = Configuration.create {
    Configuration.createEmpty() with 
        Port = 9865
        Requests = [ reitbeteiligungRequest; testRequest ]
}

let server = Server.create configuration 
server.start ()
stdin.ReadLine() |> ignore
server.stop ()