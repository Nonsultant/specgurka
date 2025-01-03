# SpecGurka

Is a collection of tools for working with the test result of an automated BDD process, based on gherkin-feature files and a test automation tool (like [reqnroll](https://reqnroll.net/)).

## GenGurka

GenGurka is a command-line tool designed to pull data from Gherkin feature files and combine it with test results in trx-format to generate a `.gurka` file. This file is used by the VizGurka web-application to visualize the test results of BDD (Behavior-Driven Development) processes.

### Key Features

- **Combines Gherkin and Test Results**: GenGurka merges the information from Gherkin feature files and test results to create a comprehensive `.gurka` file.
- **Pipeline Integration**: The tool is designed to be used within a release pipeline, such as a GitHub Action, to automate the generation of `.gurka` files.
- **Easy to Use**: GenGurka provides a simple command-line interface with options and arguments to customize the generation process. That is easily installed as a dotnet tool.

### Usage

The tool is published to GitHub: [https://github.com/Nonsultant/specgurka/pkgs/nuget/GenGurka](nuget/GenGurka) you will first need to create a classic token with 'read:package'-permission

1. **Install the Tool**: Install GenGurka as a .NET tool within your pipeline after installing .NET:

    Add github as a source:
    ```bash
    dotnet nuget add source --username [GITHUB-USERNAME] --password [GITHUB_TOKEN] --store-password-in-clear-text --name nonsultant "https://nuget.pkg.github.com/nonsultant/index.json"
    ```
    
    ```bash
    dotnet tool install gengurka --add-source nonsultant -g
    ```

2. **Generate `.gurka` File**: Navigate to the test project directory and run the tool with the appropriate options and arguments:

   Arguments:

- `-trx <path>`: Path to the trx file from the dotnet test command.
- `-o <path>` or `--output-path <path>`: Path to the output file of the .gurka file. Default is the current directory.
- `-f <path>` or `--feature-directory <path>`: Path to the directory containing the feature files. Default is the Features directory in the current directory.
- `-p <name>` or `--project-name <name>`: Name of the project the result is created from.
- `-a <path>` or `--assembly <path>`: Path to the test assembly file.
    ```bash
    gengurka -p My-Awesome-Project -trx path-to-trx-file
    ```

3. **Transfer the `.gurka` File**: The generated `.gurka` file will be created in the current directory. Transfer this file to your VizGurka application for visualization.

GenGurka simplifies the process of combining Gherkin feature files and test results, making it easier to visualize and analyze BDD test outcomes with VizGurka.
## VizGurka

VizGurka is a **ASP.NET core Razor pages** web application designed to visualize the test results of Gherkin-based BDD 
(Behavior-Driven Development) tests. It processes input from `.gurka` files,
which are generated by combining Gherkin feature files with test results, this is handled by GenGurka.

### Key Features

- **Visualization of Test Results**: VizGurka displays the results of BDD tests in a clear and organized way, making it easy to understand the status of various scenarios and features even by non-technical users.
- **Automatic File Handling**: The application automatically selects the latest `.gurka` file based on date and time from the `GurkaFiles` directory and uses it to display the test results.
- **Gherkin Syntax Support**: VizGurka supports a lot of the different kinds of Gherkin syntax, for example descriptions, rules, scenario-outline, and backgrounds.

### Usage from soruce

1. **Generate `.gurka` Files**: Use the `GenGurka` tool to generate `.gurka` files from your Gherkin feature files and trx-file test results.
2. **Place `.gurka` Files in `GurkaFiles` Directory**: Copy the generated `.gurka` files into the `GurkaFiles` directory within the VizGurka application.
3. **View Test Results**: Open the VizGurka web application to view the latest test results, which are automatically updated based on the most recent `.gurka` file. Might need a restart also.

VizGurka provides a comprehensive and user-friendly interface for monitoring and analyzing the outcomes of your BDD tests, helping you ensure the quality and reliability of your software.

### Usage from docker

1. **Generate `.gurka` Files**: Use the `GenGurka` tool to generate `.gurka` files from your Gherkin feature files and trx-file test results. It recomended to put this in a seperate folder.
2. **Pull docker image**: Find lastest image in: https://github.com/Nonsultant/specgurka/pkgs/container/specgurka%2Fvizgurka
4. **Start docker conatiner**: The app will need a host-port and to bind the folder with the gurka-file, an example:  'docker run -p 9080:8080 -v .:/app/GurkaFiles ghcr.io/nonsultant/specgurka/vizgurka:build-3' (host-port: 9090, gurka-folder: . (this folder))

VizGurka provides a comprehensive and user-friendly interface for monitoring and analyzing the outcomes of your BDD tests, helping you ensure the quality and reliability of your software.

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

* [@TheWombatKonrad](https://github.com/TheWombatKonrad): Development of SyncGurka
* [@fredidi](https://github.com/fredidi): Development of SyncGurka
* [@adrianbodin](https://github.com/adrianbodin): Development of GenGurka and VizGurka
