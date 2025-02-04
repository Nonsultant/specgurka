Feature: Manage Warehouse

  This feature is to manage the warehouse and which Category it has.

  Scenario: Change Category of the warehouse
    Given the warehouse has a Category named "Electronics"
    When I change the Category to "Clothing"
    Then the Category of the warehouse should be "Clothing"

   Scenario: Cant change Category to a Category with the same name
    Given the warehouse has a Category named "Electronics"
    When I change the Category to "Electronics"
    Then It should fail

    Scenario: Cant change Category to a Category with an empty name
    Given the warehouse has a Category named "Electronics"
    When I change the Category to ""
    Then It should fail and not let me change 