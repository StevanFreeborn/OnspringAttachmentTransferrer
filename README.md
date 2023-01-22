# Onspring Attachment Transferrer

A tool built for helping Onspring customers transfer attachments from one or more attachment/image fields from one or more records in a source app to another target app. The source and target app can be in the same instance or they can be in different instances.

**Note:**
This is an unofficial Onspring tool. It was not built in consultation with Onspring Technologies LLC or a member of their development team. This tool was developed indepdently using Onspring's existing [API .NET SDK](https://github.com/onspring-technologies/onspring-api-sdk).

## API Keys

This tool makes use of version 2 of the Onspring API. Therefore you will need API Keys to be able to utilize the tool. API keys may be obtained by an Onspring user with permissions to atleast read API Keys for your instance, using the following instructions:

1. Within Onspring, navigate to [/Admin/Security/ApiKey](/Admin/Security/ApiKey).
2. On the listing page, add a new API Key (requires Create permissions) or click an existing API Key to view its details.
3. On the details page for an API Key, click on the Developer Information tab.
4. Copy the X-ApiKey Header value from this section.

**Important:**

+ An API Key must have a status of `Enabled` in order to be used.
+ Each API Key must have an assigned Role. This role controls the permissions for requests that are made by this tool to retrieve files from fields on records in an app and transfer those files into the correct fields in the target app. If the API Keys used do not have sufficient permissions the tool won't be able to successfully transfer attachments.

## Permission Considerations

You can think of any API Key as another user in your Onspring instance and therefore it is subject to all the same permission considerations as any other user when it comes to it's ability to access a file, download it, or add it. The API Keys you use with this tool need to have all the correct permissions within your instance to access the record where a file is held and the field where the file is held as well as the field where you want to transfer the file to. Things to think about in this context are `role permissions`, `content security`, and `field security`.

## API Usage Considerations

This tool uses version 2 of the Onspring API to transfer the attachments. Currently this version of the Onspring API does not provide any endpoints to perform bulk operations for retrieving attachments, retrieving attachment information, or adding attachments.

Therefore at least three API requests must be made for each attachment you want to transfer from your instance - one request to get the attachment's information, another to get the attachments actual content, and finally one to add that attachment to the target field in the target record. There are also additional requests made to locate all the attachments you want to transfer and the location they should be transferred to. The number of additional requests is variable based on the number of records being processed and the page size of each request.

Expressed as an equation the number of api requests would be something like this:

```text
3 + (2⌈(totalRecords/pageSize)⌉) + (⌈(totalRecords/pageSize)⌉ * numberOfRecordsRetrieved) + 3((⌈(totalRecords/pageSize)⌉) * numberOfRecordsRetrieved * numberOfFieldsPerRecord * numberOfFilesPerField)
```

But as an example if you were to use this tool to transfer the attachments from 50 records each containing 10 attachments in 1 attachment field to another record using the default page size of 50 records you would end up making 1,604 requests.

You can find a simplied view of the loops involved in [requestCalculator.js](/Scripts/requestCalculator.js). You can also modify and run this script to make a rough estimation of the number of requests the tool will make on your behalf to transfer your files by modifiying the `totalRecords`, `numOfFieldsProcessed`, `numOfFilesPerField`, and `pageSize` variable values in the script.

This all being shared because it's important you take into consideration the number of requests it is going to take to transfer your attachments. If the quantity is quite considerable I'd encourage you to consult with your Onspring representative to understand what if any limits there are to your usage of the Onspring API.

## Installation

The tool is published as a release where you can download it as a single executable file for the following operating systems:

+ win-x64
+ linux-x64
+ osx-x64 (Minimum OS version is macOS 10.12 Sierra)
  + Note after downloading the executable you may need to run `chmod +x` to give the executable execute permissions on your machine.
  + Note after downloading the executable you may need to provide permission for the application to run via the your systems settings.

You are also welcome to clone this repository and run the tool using the [.NET 7](https://dotnet.microsoft.com/en-us/download) tooling and runtime. As well as modify the tool further for your specific needs.

## General Usage

When starting the tool you will always be prompted for the following configuration information:

+ `Source Instance Key`
  + This is the api key that has all the proper permsissions to retrieve the attachments from the source app's instance.
+ `Target Instance Key`
  + This is the api key that has all the proper permsissions to add the attachments to the target app's instance.
+ `Source App Id`
  + This is the id of the app where your attachments are located.
+ `Target App Id`
  + This is the id of the app where you want to transfer your attachments.
+ `Source Match Field`
  + This is the id of the field in your source app that you want to use to identify which record in your target you want to move attachments to for a given source record.
  + This field should be a number, date/time, text, auto-number, or formula field with a non-list output type.
+ `Target Match Field`
  + This is the id of the field in your target app that will hold the matching value to the source record which will identify a given target record as the record to move a soure records attachments to.
  + This field should be a number, date/time, text, auto-number, or formula field with a non-list output type.
+ `Attachment Field Mappings`
  + This is a comma separated list of field pairings separated by a pipe (`|`) delimiter that identifies attachments in what source field shoudl be moved to what target field.
  + **Example:** `1111|2222,3333|4444`
+ `Flag Field Id`
  + This is the id of the field in your source app that will be used to identify which records in the source app should be processed by the tool.
  + This should be a single select list field.
+ `Process Value`
  + This is the value of the flag field that you want to use to identify which records in the source app should be processed by the tool.
  + This value can be the list value itself or it's corresponding GUID id which can be optained by exporting the Fields & Objects report for the source app or using the Onspring API.
+ `Processed Value`
  + This is the value of the flag field that you want to use to identify which records in the sourc eapp have been processed by the tool.
  + The tool will also update the flag field with this value once a source record has been processed to indicate as much.
  + This value can be the list value itself or it's corresponding GUID id which can be optained by exporting the Fields & Objects report for the source app or using the Onspring API.

**Note**: These values can be specified in a `.json` file as key-value pairs and then passed in as a command line argument using the `--config` option. See the [Options](#options) section for  more detail.

### Obtaining Configuration Values

+ `Api Keys` can be obtained as outlined in the [API Key](#api-keys) section.
+ `App Ids` can be obtained...
  + by using the [Onspring API's](https://api.onspring.com/swagger/index.html) [/Apps](/Apps) endpoint
  + by looking at the url of the app in your browser.
  ![app-id-url-example.png](/README/Images/app-id-url-example.png)

+ `Field Ids` can be obtained...
  + by using the [Onspring API's](https://api.onspring.com/swagger/index.html) [/Fields/appId/{appId}](Fields/appId/{appId}) endpoint
  + by using an app's Fields & Objects report
  ![field-and-objects-report-example.png](/README/Images/field-and-objects-report-example.png)
  + by opening the fields configurations within Onspring.
  ![field-id-example.png](/README/Images/field-id-example.png)

## Options

The tool currently has a number of options that can be passed as command line arguments to alter the behavior of the tool. These are detailed below and can also be viewed by passing the `--help` option to the tool.

+ **Configuration File:** `--config` or `-c`
  + Allows you to specify a path to a `.json` file which contains the necessary configuration values the tool needs to run as outlined in the [General Usage](#general-usage) section.
  + See [samplesettings.json](/samplesettings.json) for an example of a properly formatted configuration file.
  + **Example usage:** `OnspringAttachmentTransferrer.exe -c samplesettings.json`
+ **Log Level:** `--log` or `-l`
  + Allows you to specify what the minimum level of event that will be written to the console while the tool is running.
  + By default this will be set to the `Information` level.
  + The valid levels are: `Debug` | `Error` | `Fatal` | `Information` | `Verbose` | `Warning`
  + **Example usage:** `OnspringAttachmentTransferrer.exe -l Debug`
+ **Page Size:** `--pageSize` or `-ps`
  + Allows you to specify the size of each page of records retrieved.
  + By default the tool will attempt to request a page size of 50 records.
  + **Example usage:** `OnspringAttachmentTransferrer.exe -ps 1`
+ **Page Number:** `--pageNumber` or `-pn`
  + Allows you to specify a limit to the number of pages of records that the tool will process when run.
  + By default there is no limit. The tool will attempt to process all records in the source app that have been flagged for processing.
  + **Example usage:** `OnspringAttachmentTransferrer.exe -pn 1`
+ **Parallel Processing:** `--parallel` or `-p`
  + Modifies the tool so that each page of records is attempted to be processed in parallel.
  + By default the tool will process the records sequentially.
  + **Example usage:** `OnspringAttachmentTransferrer.exe -p`

## Output

Each time the tool runs it will generate a new folder that will be named based upon the time at which the tool began running and the word `output` in the following format: `YYYYMMDDHHMM-output`. All files generated by the tool during the run will be saved into this folder.

Example Output Folder Name:

```text
202301212223-output
```

## Log

In addition to the information the tool will log out to the console as it is running a log file will also be written to the output folder that contains information about the completed run. This log can be used to review the work done and troubleshoot any issues the tool may have encountered during the run. Please note that each log event is written in [Compact Log Event Format](http://clef-json.org/). You are welcome to parse the log file in the way that best suits your needs.

Various tools are available for working with the CLEF format.

+ [Analogy.LogViewer.Serilog](https://github.com/Analogy-LogViewer/Analogy.LogViewer.Serilog) - CLEF parser for Analogy Log Viewer
+ [clef-tool](https://github.com/datalust/clef-tool) - a CLI application for processing CLEF files
+ [Compact Log Format Viewer](https://github.com/warrenbuckley/Compact-Log-Format-Viewer) - a cross-platform viewer for CLEF files
+ [Seq](https://datalust.co/seq) - import, search, analyze, and export application logs in the CLEF format
+ [seqcli](https://github.com/datalust/seqcli) - pretty-print CLEF files at the command-line

Example log message:

```json
{"@t":"2023-01-22T04:36:52.9588447Z","@mt":"Onspring Attachment Transferrer Started"}
{"@t":"2023-01-22T04:36:52.9757195Z","@mt":"Fetching Page {CurrentPage} of records for Source App {SourceApp}.","CurrentPage":1,"SourceApp":394}
{"@t":"2023-01-22T04:36:53.2906195Z","@mt":"Successfully retrieved {CountOfRecords} record(s) for Source App {SourceAppId}. (page {PageNumber} of {TotalPages})","@l":"Debug","CountOfRecords":11,"SourceAppId":394,"PageNumber":1,"TotalPages":1}
{"@t":"2023-01-22T04:36:53.2908753Z","@mt":"Begin processing Page {CurrentPage} of records for Source App {SourceApp}.","CurrentPage":1,"SourceApp":394}
{"@t":"2023-01-22T04:36:53.3104300Z","@mt":"Begin processing Source Record {SourceRecordId} in Source App {SourceAppId}.","SourceRecordId":3,"SourceAppId":394}
```
