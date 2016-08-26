# REDCapDotNetDETs
> Created by Paul Litwin, [Collaborative Data Services](http://cds.fredhutch.org), Fred Hutchinson Cancer Reseach Center, Seattle

## Description
This project is a .NET implementation of a set of [REDCap](https://projectredcap.org) Data Entry Triggers.
Basic attributes of this project:
- Created with Visual Studio 2015.
- .NET version: 4.5.2
- Built as an ASP.NET WebAPI web service
- Uses Log4Net for logging
- Includes unit test for Adaptive randomization code
- Extensive comments in source code

###This solution contains three WebAPI endpoints
- **DETExample** - a basic example of a .NET data entry trigger. Implemented using class  DotNetDETs/Controllers/DETExampleController.cs.
- **Adaptive** - a .NET data entry trigger that implements **Adaptive Randomization** in REDCap. Implemented using class  DotNetDETs/Controllers/AdaptiveController.cs.
- **DatabasedNotify** - a .NET data entry trigger that reacts to a saved survey and then, based on the value
of a field on the survey, adds the record to the appropriate data access group and emails the
appropriate site contact.

*See additional details of each endpoint below.*

##DETExample
This is basic example of a data entry trigger built in asp.net as a WebAPI web service. It uses common code (see below).

##Adaptive
This REDCap DET implements Adaptive Randomization per Smoak and Lin 
<http://www2.sas.com/proceedings/sugi26/p242-26.pdf>.
- One difference from the Smoak and Lin paper is that there is no run-in of simple randomization as mentioned in the paper. Instead, only the first assignment for each covariate group is randomly assigned using simple randomization. Thereafter, all subjects in that group are randomized using adaptive randomization.

##DatabasedNotify
Performs two actions based on the value of the cityField field:
1. Adds form to appropriate data access group (DAG) based on city.
2. Notifies appropriate contact at the site for that city.

Note: The DatabasedNotifyEmailsTestMode config setting of true diverts all emails to 
test recipient. Need to set to false when in production.


##Unit Test for the Adaptive randomization code
This allows you to quickly randomize a bunch of subjects to see if the adaptive randomization routine is working properly.

##Common Code
The common code under the Infrastructure folder consists of the following classes:
1. RedCapDETBModelBinder.cs is used to parse the posted values passed to the DET by REDCap. 
2. RedCapAccess.cs contains routines to read and write records to REDCap. The REDCap API code was adapted from the work of [Chris Nefcy](https://github.com/redcap-tools/nef-c-sharp).
3. Metadata.cs is a class used to store the metadata (data dictionary) from the ExportMetadata API call.
4. Messaging is used to send asynchronous emails.

##REDCap Hook used to integrate Adaptive Randomization into data form
Here is the hook code used to create a **Randomize Participant** button on our randomization form which mimics the Save and Continue button on a REDCap form. It uses the [Andy Martin REDCap Hook Framework](https://github.com/123andy/redcap-hook-framework). *This PHP code shown here is not included in the source of this .NET project.*
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
