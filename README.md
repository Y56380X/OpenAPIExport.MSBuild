# OpenAPIExport.MSBuild

MSBuild props for automatically exporting api swagger doc files.

## Getting started

* Add OpenAPIExport.MSBuild nuget package to api project
* During build the api project starts and downloads its swagger doc 
* Enjoy automatic swagger doc export

## Configuration options

Threre are some configuration options for the MSBuild task executed during api build.

* `ExportPath`: file path for saving swagger doc (default: `$(SolutionDirectory)/doc/api-doc.yaml`)
* `ServerPort`: port where the build time api runs on (default: `5005`)
