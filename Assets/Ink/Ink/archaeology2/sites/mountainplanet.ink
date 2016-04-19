VAR MOUNTAIN_PLANET_OUTSIDE = "FootOfMountain"
VAR INSIDE_LOWER_FLOOR = "InsideLowerFloor"
VAR ONE_FLOOR_UP = "OneFloorUp"

=== function prison_theory() ===
	~ return (entering_low_building_first_time.prison)


/*--------------------------------------------------------------------------------------------


		LOCATION: At the foot of a mountain cliff, whose side is clustered with low building upon low building. Each is tiny, most like a shack. This was in fact, a prison, built into an inhospitable cliff-face.


--------------------------------------------------------------------------------------------*/

=== mountain_scene  ===
= begin 
	-> set_events(-> intro)  -> goto(MOUNTAIN_PLANET_OUTSIDE) -> foot_mountain -> begin_scene -> END

= exit 
	>>> Six activated the hopper and we returned to the ship.
	-> end_scene ->->

= intro
	El and Six stand on a plain by the sheer face of a cliff; a mountain whose side has broken away. Built into the naked rock are layer after layer of dwellings.

	* 	El:		Look at that, Six. Look at it.
	 	Six:	It is quite a find, Mistress.
	*	(something) El:		There's something here, at least.
		Six:	I would say it was quite a find, Mistress.
	-
	*	{something} El:	You can't ever be sure.
		El:		Not until you've dug up a diamond or two.
	*	El:		Let's see if it has anything of value.
		~ lower(honour)
	*	El:		If this rock can hide this...
		El:		... think what else is out there, Six.
		~ raise(honour)
	-	Six:	Indeed, Mistress.
		->->

= chat 
	{ cycle:
		-	{ 
				- not over_mountain: 	-> over_mountain
				- not she_send: 		-> she_send
				- show_bracket_to_six && not unpleasant: -> unpleasant
			}
		-	<- el_qns
	}
	- ->-> 

	- (unpleasant)	
		Six:	These seems a most unpleasant planet, Mistress.
		* 	El:		It does. 
		*	El:		I think we've seen as much as it has to offer. 
		*	El:		It's a large site, Six. We can't leave it unexplored.
			Six:	I do not believe we are suitably equipped. 
	-	Six:	We are unlikely to find much of value. It appears to have stripped bare.
	-	
		*	El:		All right. Let's go back to the ship.
			Six:	Very good, Mistress.
			-> exit
		*	El:		Let's look around a little more.
			Six:	As you wish.

	-	->->
	- (over_mountain)
		Six:	If the main site is across the mountain, Mistress, we will not reach it.
		* 	El:		You can't land us closer?
			Six:	Not without significant risk of teleporting inside the rock.
		*	El:		You mean, you won't. 
			El:		I can climb a mountain.
			~ raise(tension)
			Six:	I am aware that you have legs, Mistress. 
			Six:	But I do not believe you can climb this mountain, Mistress.
		*	El:		There's a lot within reach.
			* * 	El:		And there might be a tunnel or something.
			* * 	El:		Let's not give up before we've tried.
			- - 	Six:	Indeed, Mistress.
	-	->->
	- (she_send)	
		Six: 	We can report this planet to Professor Myari.
		Six:	 I am sure she will be able to send a better equipped survey team.
		*	El:		That's good to know.
			->->
		* 	El:		And we will never see the results.
			Six:	I am positive Professor Myari will share her finds with the wider community.
			<- arrive
			* *		El:		I hope so, I really do.
			* *		El:		Professor Myari funds me, Six, but who funds her?
					Six: 	I will assume that question is one you would consider to be rhetorical.
		*	El:		It's my find, Six. I don't want to hand it over.
			Six:	If you cannot go there, Mistress, it is not your find.
	- 	->->

= el_qns
	*	{side_chat()} El:	How are we doing for time?
		Six:	We should have a few hours' of daylight still.
	*	{side_chat()} El:	Are you coping with the dust?
		Six:	For the moment, Mistress.
	-	->->

=== foot_mountain ===
= enter 
	 -> set_events(-> mountain_scene.chat)  -> set_hub (-> hub) ->->


= hub 
	*	(ms1) [Mountain Slope - Look]
		>>> The slopes were stacked with dwelling-places, one upon the other, like bubbles in a foam.

		El:		What do you make of it? 
		{bs:
			-> hardtoposit
		}
		El: A whole city?
		Six: 	It does not seem large enough to be a city, Mistress.
		* * 	El:		But definitely a settlement.
		* *		El:		Some kind of workplace?
		* * 	El:		What else could it be?
		- -		(hardtoposit) Six: 	It is hard to posit without additional evidence, Mistress.
		<- done_talking
		* * 	{not down(tension)} El:		Aren't rows and rows of houses evidence enough for you?
				{raise(tension)}
		* * 	{not up(tension)} 	El:		What sort of evidence are you thinking of?
				{lower(tension)}
		* * 	El:		It's a strange layout for a city.
				El:		No common areas, no throughfares. But, lots of dwellings.
		- - 	Six: 	I would be interested to see what lies at the top of the mountain, Mistress. 
		- - 	Six:	For instance, if there is a quarry or a mine of some kind.
		<- done_talking
		* * 	El: 	A mining camp... 
				El:		That's a good theory, Six. 
		* * 	El: 	Couldn't we see from orbit?
				Six:	A mine would be underground, Mistress.
	
	*	(bs) [Buildings - Look]
		El:		Each building is so small. 
		El:		Can't be more than single rooms. And - open to the elements?
		{hardtoposit: ->-> }
		Six:	This was clearly not a well-off city, Mistress.
		* * 	El:	If it is a city.
		* *		El: Clearly not.
		- - 	El: I doubt this rock has ever been fertile, after all.

	*	(wll) {bs} [Wall - Inspect]
		>>> I went over to one wall, and traced it with my fingertips.
		El:	There's something here. 

	*	{wll} [ Wall - Brush ]
		>>> Six handed me a brush and I began to sweep away the dust.
		El:		Some kind of sign.
		§CITY OF REFINEMENT AND CORRECTION§
		§DO NOT ENTER§
		§DANGER OF DEATH§



	*	{ms1} [Mountain Slope - Climb]
		El:		This way, Six.
		* *		El: You can handle the slope, can't you?
		* * 	El:	I'll go, you wait here.
		- - 	-> cant_let_you_go

	+	(lb) [Low Building - Explore]
		-> set_events(-> entering_low_building_first_time) -> goto(INSIDE_LOWER_FLOOR) -> inside_low_building

	*	{not show_bracket_to_six}  {one_floor_up.hub.got_bracket} [ Six - Show Bracket]
		-> show_bracket_to_six

	*	{mountain_scene.chat.unpleasant || inside_low_building} [ Path - Return to Ship]
			El: 	All right. I've seen enough.
			El:		Let's go back.
			Six:	 Mistress.
			-> mountain_scene.exit 

	- 	->->

= cant_let_you_go
	-	Six: 	This is an extensive settlement, Mistress, however...
		*	El:		However what?
		*	El:		Where's the most intact part?
			El:		{up(honour):We should be able to get the most interesting results there.|Hopefully they'll be a few valuable finds there.}
		*	El:		I don't like the look of the boulders.
			Six:	Nor do I, Mistress. There has been considerable subsidence here over some time.
	-	Six: 	We should remain on the ground level.
		Six:	I do not believe the ruins higher up will bear weight.
		*	El:		Thanks for the warning.
			Six:	I fear it is more than a warning, Mistress.
			-> cannotallow
		*	El:		I'll go up.
			El: 	You catalogue what you can down here.
			- - (cannotallow) Six:	I cannot allow you to go up there alone, Mistress. I would be unable to assist you.
			-> cantgoopts
		*	El:		Your weight, or mine?
			Six:	That is a moot point, Mistress; I will not leave your side.
			- - (cantgoopts)
			* * 	El:		You're a good companion, Six.
					Six: 	Thank you, Mistress.
			* * 	{not down(tension)} El:		Remind me why I travel with you again?
					{raise(tension)}
					Six:		Because Professor Myari insists, Mistress.
			* * 	El:		But there must be hundreds of finds to make up there.
					Six:	They will left to others with the proper equipment.
	-	->->


/*--------------------------------------------------------------------------------------------


		LOCATION: Inside one of the buildings on the ground floor


--------------------------------------------------------------------------------------------*/

=== entering_low_building_first_time ===  
= top
	{|->->}

	>>> One of the doorways was at ground level. I slipped inside.
	
	->->
	
= inside 
	{|->->}

	>>> At the far end of the room was a ladder.
	<- done_talking
	* 	El:		Look at that.
		Six:	The ladder?
		* *		(safe) El:	 	Does it look safe?
				Six:	It is almost certainly not safe, Mistress.
				Six:	The idea it could bear any weight is absurd.
		* *		El:	 	On the inside?	
				El:		Houses, connected to their neighbours, internally?
				Six:	That does suggest some other possibilities, Mistress.
				- - - 	El:	It does. 
				- - - 	(opts)
				* * * 	El: ... 	A barracks, for instance.
						Six:		
				* * * 	(prison) El:	... 	A prison.
						Six:		A most interesting possibility.
				* * *	El:	.... 	A warehouse.
						Six:		There is some suggestion of human occupancy to the room, Mistress.
						Six:		There are windows, a rough bed.
						-> opts
	-	->->



=== inside_low_building ===

= enter
	-> set_hub(->hub) -> set_events(->mountain_scene.chat) -> entering_low_building_first_time.inside -> 
	->->

= hub 
		
	+	[Doorway - Back Out]
		-> goto(MOUNTAIN_PLANET_OUTSIDE) -> foot_mountain

	*	(tl) [Ladder - Test]
		>>> I gave the lower rung of the ladder a solid pull. It held firm.
		* * 	{entering_low_building_first_time.inside.safe}	El: 	Good enough for you, Six?
				Six:	Regrettably yes, Mistress.
		* *		El:		I'll just take a quick look on the next level.
				Six:	Mistress, please...
	
	*	(cl) {tl} [Ladder - Climb]
		>>> A short climb took me into the room above, little different from the one below.
		-> goto(ONE_FLOOR_UP) -> one_floor_up
		
	*	 {one_floor_up.hub.got_bracket} [ Six - Show Bracket]
		-> show_bracket_to_six

	-	->->

=== show_bracket_to_six ===
	-	* 	El:		Six? What do you make of this?
			Six:	Quantium. Not very refined. Strong, rather than supple.
			Six:	Where did you find it?
			* * 	El:	I pulled it out of the wall.
					Six:	I am surprised, Mistress.
					Six:	Surely it is part of the site?
					* * * 	El:	 	It's hardly a large item. 
							~ lower(honour)
							-> mostlikely
					* * * 	El:		We take artefacts off-site all the time, Six.
						- - - - (mostlikely)
							Six:	Most likely you have damaged the wall as well.
					* * * 	El:		I'll put it back.
							Six:	I do not believe it will go back, Mistress, the bolt was barbed.
							
			* * 	El:	Lying on the floor in the room above. 
					Six:	Most curious. The bolts appear barbed.

			- - 	Six:	I believe this was intended specifically to resist being pulled free.
			- - (opts)
			* * 	El:		So a mount for a chain?
					Six:	An ankle chain perhaps, yes, Mistress.
			* * 	(small) El:		It's small, though.
					Six:	Indeed. Intended for a child, perhaps.
			* * 	{small} El:	Is this a prison or a workcamp?
					Six:	Most likely both, Mistress, though I cannot detect what was being worked on.
					Six:	There are no interesting minerals, no radiation.

			* * 	{opts > 1} 	El:		I've seen enough.
					Six:	Mistress.
					>>> Six slipped the bracket away into one of his many compartments.
	-	->->
	


/*--------------------------------------------------------------------------------------------


		LOCATION: One floor up inside the mountain side village


--------------------------------------------------------------------------------------------*/

=== one_floor_up ===
= enter
	§ El is in a room in the mountainslope one floor up. Six is below, watching anxiously.
	-> set_hub(->hub) -> set_events(->NPC) ->-> 
	

	
= NPC
	{
		- not please_be_careful: -> please_be_careful
	}
	- ->->
	- (please_be_careful) Six:	Please be careful, Mistress!
		*	El:		Of course I'll be careful.
		*	El:		Stop worrying.
		*	El:		How's the floor looking from down there?
			El:		Any cracks? 
			Six:	None I can see, Mistress.
			Six:	There is a most ominous dust trickle, however.
	-	->->

= coming_down
	*	El: 	All right. I'm coming down.
		Six:	I am pleased to hear it, Mistress.
	- ->->

= hub 
	*	[Ladder Top - Climb Down]
		El:		Right, I'm coming back down.
		-> set_events(->coming_down) -> goto(INSIDE_LOWER_FLOOR) -> inside_low_building

	*	[Window - Look Through]
		>>> 	I went over to the window and looked out over the plain.
		El:		A house? Or something else..?

	*	[Low Shelf - Inspect]
		El:		Carved out of the rock. A bed? Too low for a workbench.
		El:		There's a metal bracket here, in the wall.

	*	(mb) [Metal Bracket - Examine]
		>>> I tugged at the hanging bracket gently. It had been hammered hard into the rock that still held fast.
		El:		A fitting for something.
		{prison_theory():
			El:		A chain, perhaps. A prison after all, then.
		- else:
			El:		A chain, perhaps?
		}

	*	(got_bracket) {mb} [Metal Bracket - Pull]
		
		El:	I could probably work it free...
		>>> I twisted, tugged, and out it came.
		El:	Definitely worn. Must have been in use for some time. 
		#pocketsbracket

	-	->->




	