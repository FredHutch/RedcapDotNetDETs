# REDCapDotNetDETs
> Created by Paul Litwin, [Collaborative Data Services](http://cds.fredhutch.org), Fred Hutchinson Cancer Reseach Center, Seattle

## Description
This project is a .NET implementation of a set of [REDCap](https://projectredcap.org) Data Entry Triggers.
Basic attributes of this project:
- Created with Visual Studio 2015.
- .NET version: 4.5.2
- Built as an ASP.NET WebAPI web service

###This solution contains two WebAPI endpoints
- **DETExample** - a basic example of a .NET data entry trigger. Implemented using class  DotNetDETs/Controllers/DETExampleController.cs.
- **Adaptive**  - a .NET data entry trigger that implements **Adaptive Randomization** in REDCap. Implemented using class  DotNetDETs/Controllers/AdaptiveController.cs.
*See additional details of each endpoint below.*

##DETExample
This is basic example of a data entry trigger built in asp.net as a WebAPI web service. It uses common code (see below).

*(More details to come)*

##Adaptive
This REDCap DET implements Adaptive Randomization per Smoak and Lin <http://www2.sas.com/proceedings/sugi26/p242-26.pdf>.
- One difference from the Smoak and Lin paper is that there is no run-in of simple randomization as mentioned in the paper. Instead, only the first assignment for each covariate group is randomly assigned using simple randomization. Thereafter, all subjects in that group are randomized using adaptive randomization.

*(Much more detail to come!)*

##Unit Test for the Adaptive randomization code
This allows you to quickly randomize a bunch of subjects to see if the adaptive randomization routine is working properly.

*(More details to come!)*

##Common Code
The common code is used to parse the posted values passed to the DET by REDCap. It also contains routines to read and write records to REDCap.

*(Much more details to come!)*

##REDCap Hook used to integrate Adaptive Randomization into data form
Here is the hook code used to create a **Randomize Participant** button on our randomization form which mimics the Save and Continue button on a REDCap form. It uses the [Andy Martin REDCap Hook Framework](https://github.com/123andy/redcap-hook-framework).
```
<?php
	// Saved to a file named redcap_data_entry_form.php and placed in the hooks folder for the appropriate project
	switch ($instrument) {

        case "randomization":
			print '<script type="text/javascript">$(function() {$("input[name=\'pc_rnd_ready\']").replaceWith("<input type=\'button\' id=\'btnRandomize\' name=\'submit-btn-savecontinue\' style=\'font-weight:bold;font-size:12px;margin:1px 0;\' onClick=\'dataEntrySubmit(this);return false;\' value=\'Randomize Participant\' />"); });</script>';
			break;

        default:
		//nothing to do
	
	}
?>
```
