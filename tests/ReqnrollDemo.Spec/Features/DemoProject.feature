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

Rule: When firstname and lastname are alike should it only appear once

	Scenario: Combine alike firstname and lastname
		Given firstname is "Otis"
		And lastname is "Otis"
		When the two names are combined
		Then the result should be "Otis"