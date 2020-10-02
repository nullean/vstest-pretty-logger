module Paths

open System
open System.IO

let ToolName = "vstest-pretty-logger"
let Repository = sprintf "nullean/%s" ToolName

let AssemblyName = "Nullean.VsTest.Pretty.TestLogger"

let Root =
    let mutable dir = DirectoryInfo(".")
    while dir.GetFiles("*.sln").Length = 0 do dir <- dir.Parent
    Environment.CurrentDirectory <- dir.FullName
    dir
    
let RootRelative path = Path.GetRelativePath(Root.FullName, path) 
    
let Output = DirectoryInfo(Path.Combine(Root.FullName, "build", "output"))

let ToolProject = DirectoryInfo(Path.Combine(Root.FullName, "src", ToolName))
