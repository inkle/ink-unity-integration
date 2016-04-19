
// planet names are flag indexes
VAR HYAF = 1

VAR planets_heard_of = 0

=== function heard_of(planetN) ===
	~ return testflag(planets_heard_of, planetN)
