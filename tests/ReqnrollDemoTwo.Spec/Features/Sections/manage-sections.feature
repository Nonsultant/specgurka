Feature: Manage Sections

  As a Manager
  I want to manage sections within the warehouse
  So that the warehouse structure remains organized and up-to-date

  - This is important
  - This is also important

  Rule: Removing and renaming sections

    This rule is **very** important to the Manager.
    Should **only** be *useful* to the Manager.

    Scenario: Remove an existing section

      **This** scenario *might* not be used to much.

      Given a section "Incoming" exists in the warehouse
      When I remove the section "Incoming"
      Then "Incoming" should no longer exist in the list of sections

    Scenario: Rename an existing section
      Given the section "Outgoing" exists in the warehouse
      When I rename the section "Outgoing" to "Exports"
      Then the section name should be updated to "Exports"
      And "Outgoing" should no longer exist in the list of sections for the warehouse

  Rule: Adding sections

    Background:

      **This** background applies to **all** scenarios in *this* rule.

      Given I am logged in as an admin user with permissions to manage sections
      And I go to the "Manage Sections" page