# REDCapDotNetDETs
> Created by Paul Litwin, Collaborative Data Services, Fred Hutchinson Cancer Reseach Center, Seattle

## Description
This project is a .NET implementation of a set of [REDCap](https://projectredcap.org) Data Entry Triggers.
Basic attributes of this project:
- Created with Visual Studio 2015.
- .NET version: 4.5.2
- Built as an ASP.NET WebAPI web service

###This solution contains two WebAPI endpoints
- DETExample -- a basic example of a .NET data entry trigger. Implemented using class  DotNetDETs/Controllers/DETExampleController.cs.
- Adaptive  -- a .NET data entry trigger that implements **Adaptive Randomization** in REDCap. Implemented using class  DotNetDETs/Controllers/AdaptiveController.cs.
