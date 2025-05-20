@ignore
@draft
Feature: Manage Company
This features are for managing the company on a higher level.
Mostly for managing the CEO of the company.

![image]("~/images/favgurka.png")

# This will probably not be used, but it CEO can be changed.
Scenario: Change CEO of the company
Given the company has a CEO named "Johnny Johnson"
When  I change the CEO to "Jane Doe"
Then  the CEO of the company should be "Jane Doe"

Scenario: Cant change CEO to a person with the same name
Given the company has a CEO named "John Doe"
When  I change the CEO to "John Doe"
Then  It should fail

Scenario: Cant change CEO to a person with an empty name
Given the company has a CEO named "Joel Johnson"
When  I change the CEO to ""
Then  It should fail and not let me change
