Feature: Manage Employees

  As an HR manager
  I want to add, update, and remove employees within sections
  So that employee records are accurate and up-to-date in the warehouse

  Background:

    This background applies to **every** scenario under it.
    Super important.

    Given I am logged in as an HR manager
    And I am on the "Manage Employees" page
  @ignore @smoke
  Scenario: Add a new employee to a section

    Testing to see if this *works*.

    Given the section "Import" exists with the following employees:
      | Name       | Role             |
      | Jane Doe   | ForkliftOperator |
      | John Smith | Janitor          |
    When I add an employee "John Doe" with the role "Manager" to the "Import" section
    Then "John Doe" should be added to the list of employees in the "Import" section

  Scenario: Remove an employee from a section
    Given an employee "John Doe" exists in the "Import" section
    When I remove "John Doe" from the "Import" section
    Then "John Doe" should no longer appear in the list of employees in the "Import" section