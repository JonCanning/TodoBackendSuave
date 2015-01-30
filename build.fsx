#r "packages/FAKE/tools/FakeLib.dll"

open Fake

// Values
let solutionFile = "TodoBackendSuave.sln"

//Targets
Target "Default" (fun _ -> 
  !!solutionFile
  |> MSBuildRelease "" "Build"
  |> ignore)

// start build
RunTargetOrDefault "Default"