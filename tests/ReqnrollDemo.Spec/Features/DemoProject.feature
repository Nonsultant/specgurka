Feature: DemoProject

Combine two strings to get diffent results

Scenario: Combine firstname and lastname
	Given firstname is "Otis"
	And lastname is "Redding"
	When the two names are combined
	Then the result should be "Otis Redding"


Scenario: Combine firstname and lastname with error
	Given firstname is "Otis"
	And lastname is "Redding"
	When the two names are combined
	Then the result should be "Otis Reddin"