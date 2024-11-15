Feature: Manage Departments

As a CEO
I want to manage departments within the company
So that the company structure remains organized and up-to-date

  Scenario: Remove an existing department
    Given a department "Human Resources" exists in the company
    When I remove the department "Human Resources"
    Then "Human Resources" should no longer exist in the list of departments

  Scenario: Rename an existing department
    Given the department "Tech" exists in the company
    When I rename the department "Tech" to "Technology"
    Then the department name should be updated to "Technology"
    And "Tech" should no longer exist in the list of departments for the company

  Scenario: Add a new department with no name should fail
    Given I want to add a new department to the company
    When I add a department with an empty name
    Then It should fail with the message "Department name cannot be empty"

  Scenario: Add a new department
    Given I want to add a new department to the company
    When I add the department "Embedded Systems"
    Then the department should be added to the list of departments in the company

  Scenario: Add a new department with the same name should fail
    Given I want to add a new department to the company
    When I add the department "Embedded Systems"
    Then the department should be added to the list of departments in the company
    When I add the department named "Embedded Systems"
    Then It should fail with the message "Department already exists"
