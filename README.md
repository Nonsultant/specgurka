# SpecGurka

Is a collection of tools for working with the test restresult a BDD process.

## GenGurka

Pulls data from Gherkin files and combine it with test results into a '.gurka'-file.

## VizGurka

A webapplication to show the test result of the gherkin, the vizualtion is based on input from '.gurka'-files.

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
