# SpecGurka

Is a collection of tools for working with the test restresult a BDD process.

## GenGurka

Pulls data from Gherkin files and combine it with test results into a '.gurka'-file.

The data is a combination of the .features files and the test result in trx-format.

GenGurkas main purpose it to be used inside a release-pipeline like a Github Action.

To use GenGurka:

1. install the tool inside the pipeline, after you installed dotnet:

```bash
  dotnet tool install gengurka
```

2. navigate to the test project you want to generate a .gurka file from. Then run the tool, options and arguments
   for the tool can be found with the `--help` as an argument.

```bash
  gengurka -p My-Awesome-Project
```

3. this will create a .gurka file in the current directory, that you can then transfer to your VizGurka application.

## VizGurka

A webapplication to show the test result of the gherkin, the vizualtion is based on input from '.gurka'-files.

Inside VizGurka there is a folder called `GurkaFiles`, this folder is where you put your gurka files created in the
pipeline. 

VizGurka will automatically take the latest .gurka file based on date and time, and display the results in the web application.



## SyncGurka

A CLI tool to sync feature files written using Gherkin syntax with a work-system (eg. Azure DevOps, GitHub Issues, Monday).

The tool is able to make sure that all feature files have a mapping aginst a work-item and that the data in that system is aligned against the feature file.

It can check the mapping of both features and scenarios to user stories to an workitem service. And make use that the id aligns (the workitem exists).

When creating automated tests based on the scenarios within the features (eg using Specflow), is it possible to update the feature in the workitem service with the result.

The system consist of two modes. A commandline argument can be used to select mode (if no argument is provided, should the defult mode just run)
* Mode 1 (default): Verify and validate gherking-files against workitem service
* Mode 2: Mode 1 + Update workitem in service with results of automated test (eg. Specflow)

# Thanks

This tools is orginaly developed as an student internship during the fall 2022 by:

* [@TheWombatKonrad](https://github.com/TheWombatKonrad)
* [@fredidi](https://github.com/fredidi)
