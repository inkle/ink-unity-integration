VAR ELBORETH_MID_STREET = "ElborethMidStreet"
 
=== elboreth_scene ===
= begin
	-> set_events(-> intro) -> goto(ELBORETH_MID_STREET) -> elboreth_street -> begin_scene ->
	-> END

= intro
	§ El and Six stand on a dusty street on the low-rent planet of Elboreth. A grimy tunnel mouth leads down towards underground apartments; a few other shops are visible including the Blue Monkey bar.

	>>> Elboreth.
	>>> I shouldn't be living on a planet like this, so the Professor says.
	>>> She lives in a tower-room on Iox with a view across the Clouds.
	>>> My place is not like that.

	->->



=== elboreth_street ===
= enter
	 -> set_hub(-> hub) -> set_events(-> NPC)
	 ->->

= hub 
	*	(bm) [Blue Monkey - Look]
		>>> 	My friend Timor runs the Blue Monkey, a trading and drinking spot.

	+	{bm} [Blue Monkey - Enter]
		El: 	Let's go see if there's anyone about.
		-> goto(BAR_DOOR) -> the_blue_monkey

	*	[Passersby - Look]
		>>> 	This place isn't great but the people here don't seem to realise.
		>>> 	The local greeting is a slap on the back. 
		>>>		It's like they're saying, "Well done! You lasted another day!"

	*	(at) [Apartment Tunnel - Look]
		>>> 	Surface space is at a premium, so most dwellings are underground. 
		>>> 	They pipe in the air room by room, so you can smell everyone above you.
	
	+	(gtunnel) {at}  [Apartment Tunnel - Enter]
		El:		Come on. Let's go home.
		-> goto(OUTSIDE_ELS_DOOR) -> apartment_corridor
		

	-	<- NPC  // note, this inserts NPC *on top of* movement
	-	->->

= NPC
	{
		-	not tired && not hub.gtunnel: 	-> tired
		-	not fencing: -> fencing
	}
	- 	-> DONE
	- (tired)
			Six: 	You must be tired, Mistress.
			*	El:	 	I want to look around a little first. 
				El:		See who's about.
				Six:	Of course, Mistress. 
				Six:	I will take the opportunity to gather some solar energy.
				§ Six lifts his head as though turning his face to the sun.
			*	El:		We'll have plenty to do in the morning.
				* * 	El:		Might as well get some rest.
						Six:	This way, Mistress.
						-> goto(OUTSIDE_ELS_DOOR) -> apartment_corridor

				* * 	El:		But I need a drink first. 
						-> goto(BAR_DOOR) -> the_blue_monkey
			- 	->->

	- (fencing) 
			Six:  	Mistress? Do you wish me to locate a purchaser for our lesser artefacts?
			Six: 	I am picking up some vendor beacons.
			*	El:		Keep your voice down.
				~ lower(honour)
				El:		It's not good for my reputation, selling these things on.

			* 	El:		I hate selling this stuff.
				~ raise(honour)
				El:		It lies in the dirt for four thousand years, and then I just fence them.
			-	Six:	I would suggest these artefacts bring more happiness once sold.
				*	El:		To the person who buys them, only.
					~ raise(honour)
				* 	El:		Surely they belong in a museum.
					~ raise(honour)
					El:		Where everyone can see them.
					Six:	The university museum currently contains over fifteen hundred similar rings, Mistress.
					* *		El:		I suppose that's true. 
					* *		El:		So they don't pay as well.
							~ lower(honour)
							El: 	I know, I know.
					* *		El:		That's fifteen hundred times someone didn't fence a ring.
							Six:	I wonder how many times they did?
				 	
			-	->->



