

=== game_intro_voiceover === 

= intro 
	The vastness of space. The ship slowly sails forward. El talks in v/o.

	-> introlines1 -> 
	*	[NEXT]
	-	-> introlines2 ->
	*	[NEXT]
	-	-> introlines3 ->
	*	[NEXT]
	-	-> introlines_intro_end -> main

= introlines1
	>>> 	The Nebula. 
	>>>		A pool of worlds, bound together by swirling winds.
	->->

= introlines2
	>>> 	An uneasy, divided place - the larger worlds ruling over a hundred scattered fiefdoms, without enough resources to go round. 
	>>>		Ships ply the trade-winds, but piracy is commonplace. The law on one world is outrage on an another.
	->->

= introlines3
	>>>		It was not always like this. Once, the Nebula was an Empire, that reached from Iox to Elboreth and beyond. 
	>>>		Once, great ships sailed as far as the Outer Reach. 
	>>>		There are tiny stars in the sky there now, that shine with unnatural light, beyond the winds. 
	->->

= introlines_intro_end
	>>> 	And some, not too far...
	->->	

= main
	Camera pulls back into the flight deck of the ship. El and Six sit, glued to the viewport.
	
	Six: 	The planetoid is approaching, Mistress. 
	Six:	Do you want me to land? 
	- (opts)
	*	El:		Is the signal still strong?
		Six:	It has been growing stronger as we approach.
		-> opts

	*	El:	 	We'll scan from orbit first. 
		Six: 	Several structures, Mistress. 
		* *		El: 	What kinds of structure?
				Six:	Mostly attendant buildings on the central temple. 
				* * *	El:	 	And the temple?
						Six:	It would appear most extensive.
						Six:	Are you prepared, Mistress?
						-> opts
				* * * 	El:		A full city?
						-> canttell
		* * 	El: 	Stone? Metal?
			- - - (canttell)
				Six: 	I cannot tell, Mistress. 
		* * 	El: 	Take us down.

	*	El: 	I'll do it. 
		Six: 	My apologies, but my Ethical Core will not allow that. 		
		- - (landingopts)
		* * 	El: 	Too rough? 
				Six: 	Indeed, Mistress.
				-> landingopts
		* * 	El: 	Go ahead then.
				Six: 	Thank you, Mistress. 
		* * 	El:		Have I still not turned that off? 
				Six: 	It would kill me if you tried. 
				* * * 	El:		The fault of my own Ethical Core, then.
						{lower(tension)}

				* * * 	El:		How sure are you?
						{raise(tension)}
						Six: 	Please, Mistress. I do not like to contemplate my own termination.
						* * * *	El: 	No one does. 
								Six: 	I shall take that as an agreement that I should do the landing.
						* * * 	El:		You can consider it a threat, if you like.
								{raise(tension)}
								Six: 	I have always found your humour to be deficient, Mistress.
								Six: 	I will now land the ship.
				- - -	El: 	Land the ship.

	*	El:		Take us down.
		~ lower(tension)
		Six: 	Very good.

- 	Six: 	 Please excuse me for a minute.

	§ The ship lands. Cue credits!

	->->

