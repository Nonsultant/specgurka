@feature
Feature: Manage Departments
As a CEO
I want to manage departments within the company
So that the company structure remains organized and up-to-date

- This is important
- This is also important

Here is a link for testing: [Reqnroll](https://reqnroll.net)

@rule
@ignore
Rule: Removing and renaming departments
This rule is **very** important to the CEO.
Should **only** be *useful* to the CEO.

@scenario
Scenario: Remove an existing department
**This** scenario *might* not be used to much.

Given a department "Human Resources" exists in the company
When  I remove the department "Human Resources"
Then  "Human Resources" should no longer exist in the list of departments

Scenario: Rename an existing department
Given the department "Tech" exists in the company
When  I rename the department "Tech" to "Technology"
Then  the department name should be updated to "Technology"
And   "Tech" should no longer exist in the list of departments for the company

Rule: Adding departments

Background:
**This** background applies to **all** scenarios in *this* rule.

Given I am logged in as an admin user with permissions to manage departments
And   I go to the "Manage Departments" page

Scenario: Add a new department with no name should fail
Given I want to add a new department to the company
When  I add a department with an empty name
Then  It should fail with the message "Department name cannot be empty"

Scenario: Add a new department
Given I want to add a new department to the company
When  I add the department "Embedded Systems"
Then  the department should be added to the list of departments in the company

Scenario: Add a new department with the same name should fail
Given I want to add a new department to the company
When  I add the department "Embedded Systems"
Then  the department should be added to the list of departments in the company
When  I add the department named "Embedded Systems"
Then  It should fail with the message "Department already exists"
