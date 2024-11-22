Feature: Manage Employees

  As an HR manager
  I want to add, update, and remove employees within departments
  So that employee records are accurate and up-to-date in the company

  Background:
    Given I am logged in as an HR manager
    And I am on the "Manage Employees" page

  Scenario: Add a new employee to a department
    Given the department "Technology" exists with the following employees:
      | Name       | Role      |
      | Jane Doe   | Engineer  |
      | John Smith | Contractor |
    When I add an employee "John Doe" with the role "Engineer" to the "Technology" department
    Then "John Doe" should be added to the list of employees in the "Technology" department

  Scenario: Remove an employee from a department
    Given an employee "John Doe" exists in the "Technology" department
    When I remove "John Doe" from the "Technology" department
    Then "John Doe" should no longer appear in the list of employees in the "Technology" department