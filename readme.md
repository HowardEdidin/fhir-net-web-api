
|STU3|
|---|

## Introduction ##

This is an unofficial WebAPI controller implementation for exposing a [HL7 FHIR][fhir-spec] on the Microsoft .NET (dotnet) platform.
It even supports data compression handling out of the box

This library provides:

* An implementation of an ApiController for the STU3 FHIR specification
* An interface for the System Service
* An interface for the Resource Service
* A partial example implementation of a fhir server CRUD that just writes files to C:\Temp\demoserver
* A unit test project that utilizes the FhirClient NuGet packages to test the example Service
* Support for both Owin (.NET 4.7+) and AspNetCore

> **Note:** Project currently uses the 0.96.0 version of the FHIR NET API assemblies - due to some 
> compatibility issues with the new parsers.

The library depends on several NuGet packages (notably):

* *Core* (NuGet packages starting with `Hl7.Fhir.<version>`) - contains the FhirClient, resource object models and parsers
* *Specification* (NuGet packages starting with `Hl7.Fhir.Specification.<version>`) - functionality to work with the specification metadata and validation
* *FhirPath* (NuGet package `Hl7.FhirPath`) - the FhirPath evaluator, used by the Core and Specification assemblies
* *Support* (NuGet package `Hl7.Fhir.Support`) - a library with interfaces, abstractions and utility methods that are used by the other packages
* *Owin*

**IMPORTANT**
Once things settle in, the HL7.Fhir.WebApi.STU3 project will be created into a NuGet package.
Before installing one of the NuGet packages (or clone the repo) it is important to understand that HL7 has published several updates of the FHIR specification,
each with breaking changes - so you need to ensure you use the version that is right for you:

* [R4][r4-spec] (published December 2019) latest release, no current support in this library.
* [STU3][stu3-spec] (published March 2017) increasing use, support in alpha by this library.
* [DSTU2][dstu2-spec] (published October 2015) in widespread use, not planning to supported by this library.

## Getting Started ##

To create your own server, copy the Hl7.DemoFileSystemFhirServer example project, then start replacing the code in the
DirectorySystemService and DirectoryResourceService classes.
Depending on your implementation needs, you may have one or more Resource classes.

(Choose either the Owin if you have a .net 4.7+ project, or the aspnetcore project if using netcore)

## Support ##

TBD
For questions and broader discussions, we use the .NET FHIR Implementers chat on [Zulip][netapi-zulip].

## Contributing ##

We are welcoming contributors!

If you want to participate in this project, we're using [Git Flow][nvie] for our branch management, so please submit your commits using pull requests no on the develop branches mentioned above!

### GIT branching strategy ###

- [NVIE](http://nvie.com/posts/a-successful-git-branching-model/)
- Or see: [Git workflow](https://www.atlassian.com/git/workflows#!workflow-gitflow)

[netapi-docu]: http://ewoutkramer.github.io/fhir-net-api/docu-index.html
[netapi-zulip]: https://chat.fhir.org/#narrow/stream/dotnet
[fhir-spec]: http://www.hl7.org/fhir
[r4-spec]: http://www.hl7.org/fhir/r4
[stu3-spec]: http://www.hl7.org/fhir/stu3
[dstu2-spec]: http://www.hl7.org/fhir/dstu2
[fhirpath-spec]: http://hl7.org/fhirpath/
