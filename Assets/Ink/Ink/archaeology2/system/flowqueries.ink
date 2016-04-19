/* -------------------------------------------------


	Flow queries


------------------------------------------------- */	

// Where did you get here from?

=== function came_from(-> x) ===
	~ return (TURNS_SINCE(x) == 0)

// Three kinds of basic query, not two!

// -- recent?  - ie. have you seen this within the last couple of turns 
// -- not recent? - ie. have you never seen it, or you saw it a little while ago 
// -- seen_not_recently - ie. you have seen it, but it was some time ago

=== function recent_within(-> x, number_of_turns)
	~ return TURNS_SINCE(x) >= 0 && TURNS_SINCE(x) <= number_of_turns

=== function seen_recently(-> x)
	~ return recent_within(x, 3)

=== function seen_reasonably_recently(-> x)
	~ return recent_within(x, 7)

=== function seen_but_not_recently(-> x) ===
	~ return TURNS_SINCE(x) > 3

=== function seen_some_time_ago(-> x) ===
	~ return TURNS_SINCE(x) > 20

=== function seen_more_recently_than(-> a, -> b) ===	
	// have you seen "a" more recently than you've seen "b"?
	// if you've never seen b, this is always true
	// In particular, if you've seen neither, this returns true
	// Note that seen *equally recently* returns false
		~ return TURNS_SINCE(a) < TURNS_SINCE(b)  || TURNS_SINCE(b) < 0 :

=== function seen_after(-> a, -> b) ===			
	// have you seen a since you last did b?
	// if you've never done a, false; if you've never done b, it's "have you done a"?
	// but note this is intended for cases where you've definitely done -> b
	// if equal time since, it's considered true (because b is a "baseline")
	{
		- TURNS_SINCE(a) < 0:	~ return false
		- TURNS_SINCE(b) < 0:	~ return true  
			// because false is covered by the line above (!)
	}
	~ return TURNS_SINCE(a) <= TURNS_SINCE(b)

/* -------------------------------------------------
	Choice queries
------------------------------------------------- */


=== function silent() ===
	~ return (CHOICE_COUNT() == 0)

=== function mostly_silent() === 
	~ return (CHOICE_COUNT() <= 1)

=== function semi_random() ===
	~ return (CHOICE_COUNT() mod 2 == semi_random mod 2)

=== function side_chat() ===
	~ return semi_random() && mostly_silent()