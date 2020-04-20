module Static
open System
open System.IO
open System.Text
open ResponseData
open Session

let private checkFile directory url = 
    let rawUrl = 
        match url |> String.indexOfChar '#' with
        | Some pos -> url |> String.substring2 0 pos
        | None -> url

    let url = 
        match rawUrl |> String.indexOfChar '?' with
        | Some pos -> rawUrl |> String.substring2 0 pos
        | None -> rawUrl

    let isDirectory = url.EndsWith "/"

    let unescapedUrl = Uri.UnescapeDataString url
    
    let relativePath = 
        let relativePath = if Path.DirectorySeparatorChar <> '/' then unescapedUrl.Replace ('/', Path.DirectorySeparatorChar) else unescapedUrl
        relativePath.Substring 1
    let path = Path.Combine (directory, relativePath)
    let path = 
        if Path.IsPathRooted path then
            path
        else
            Path.Combine(Directory.GetCurrentDirectory(), path)

    if File.Exists path then 
        path
    elif not isDirectory then
        ""
    else
        let file = Path.Combine (path, "index.html")
        if File.Exists file then file else ""

let asyncRedirectDirectory directory url responseData = async {
    let path = checkFile directory url
    if path = "" then
        do! Response.asyncSendNotFound responseData
    elif responseData.requestData.header.host.Value <> "" then
        let response = "<html><head>Moved permanently</head><body><h1>Moved permanently</h1>The specified resource moved permanently.</body</html>"
        let responseBytes = Encoding.UTF8.GetBytes response
        let redirectHeaders = 
            sprintf "%s 301 Moved Permanently\r\nLocation: %s%s\r\nContent-Length: %d\r\n\r\n"
                responseData.response.Value responseData.requestData.urlRoot.Value url responseBytes.Length
        let headerBytes = Encoding.UTF8.GetBytes redirectHeaders 

        do! responseData.requestData.session.networkStream.AsyncWrite (headerBytes, 0, headerBytes.Length)
        do! responseData.requestData.session.networkStream.AsyncWrite (responseBytes, 0, responseBytes.Length)
}

let rec asyncServeStaticUrl directory requestData url = async {
    let responseData = create requestData
    let file = checkFile directory url
    if file <> "" then  
        do! Files.asyncSendFile file responseData
    elif not (url.EndsWith "/") then
        do! asyncRedirectDirectory directory (url + "/") responseData
    else
        do! Response.asyncSendNotFound responseData
}

let asyncServeStatic directory requestData = 
    asyncServeStaticUrl directory requestData requestData.header.url

let private serveStatic directory webPath (requestSession: RequestSession) = async {
    match requestSession.Url |> String.startsWith webPath with
    | true -> 
        do! asyncServeStatic directory (requestSession.RequestData :?> RequestData.RequestData)
        return true
    | _ -> return false
}

let private asyncServeFavicon iconPath (requestSession: RequestSession) = async {
    match requestSession.Url with
    | "/favicon.ico" ->
        let requestData = requestSession.RequestData :?> RequestData.RequestData
        let responseData = create requestData
        if File.Exists iconPath then
            do! Files.asyncSendFile iconPath responseData
        else
            do! Response.asyncSendNotFound responseData
        return true
    | _ -> return false
}

let useStatic directory webPath = serveStatic directory webPath
let useFavicon iconPath = asyncServeFavicon iconPath

