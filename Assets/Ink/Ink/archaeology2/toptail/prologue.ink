

VAR FLIGHTDECK = "Flight Deck"
VAR PASSAGEWAY_FORK = "Fork"
VAR PASSAGEWAY_LEFT_JOIN = "LeftJoinUp"
VAR PASSAGEWAY_RIGHT_JOIN = "RightJoinUp"
VAR OUTSIDE_CAVE = "OutsideCave"
VAR RUNNING_DOWN_DUNE = "DuneSlope"



TODO: 	we will return to Alberthath later on and go far enough below to find a foot sticking out from under some shifted rubble. It has clearly been there some time, preserved in the unforgiving, bacteria-free environment. 
TODO:	Six may or may not assist in digging it up, but either way, El will soon find something she recognises...	


/*-------------------------------------------------



	LOCATION: The Cabin

	Bed in one corner. Door to flight deck.
	Assortment of items, bits of El's collection.


-------------------------------------------------*/


=== function done_some_things()
	~ return (prologue_scene.hub.done >= 2)


=== prologue_scene ===
= intro

	-> set_hub(-> hub) -> set_events( -> NPC) ->

	{ stopping:
		- -> top ->
		- 	
			Scene returns to the berth.
			El: 	Ah, yes. I remember now.
	}

	-> begin_scene ->
	-> END

= top
	The background slowly moves into focus. A spaceship berth. A robot face looms into view, but we do not see his body.
	-> wakes

= wakes
	Six: 		Ah, Mistress. 
	Six:		You're awake.
	- (opts)
		*	(where) El:		Where am I?
			Six:	Back aboard the /Nautilius/, Mistress. 
		*	El: 	What happened?
		*	(who) El:		Who's there?
			Six: 	Oh dear. 
			Six:	I fear you did not escape the rubble of the collapsing ruins quite as I had hoped.
				-> opts
		*	{who} 	El: Collapsing ruins?
			Six: 	Indeed. They are now somewhat *more* ruined, I am afraid.
				-> hopper
	-	Six: 	Your investigation {who:on the asteroid|of the Alberthath ruins} did not go... as hoped.
	-	(hopper) Six: 	I was forced to activate the hopper.
		*	El:		My head...
			El:		Did something hit me?
			Six: 	Thankfully no, Mistress. That was avoided by moments.

		*	El:		I don't remember anything.
			Six:	That is quite common.

		*	El:		The hopper?
			Six:	Indeed. It has most likely left you dehydrated. 
			* * 	El: 	My head is aching. 
			* *		El: 	I'll be all right.
			* * 	El:		Just tell me what happened down there.
					Six:	I will, shortly, Mistress.
			* * 	{not where} El:		What's going on? Where am I?
		*	{not where} 	El: 	Where am I? 
			Six:	You are back aboard the /Nautilus/. 
	-	Six:	Please wait here while I get you some water.
		The robot hoves out of view. El sits up.
		~ 	with_six = false
	-	->->

= hub
	*	[Mirror - Look]
		>>> I put a hand to the side of my head and winced.

		El: 	There's a bump coming, all right. 
		El:		What happened down there?

	*	[Room - Look]
		El:		I know this place all right. 
		El:		There must be sandgrubs in here by now. 

	*	(sh1) [Stone Head - Look]
		El:		What are you looking at? 
		Head: 	I do not understand the question.

	*	{sh1} [Stone Head - Look]
		El:: 	Found in the ruins at Makhesh. I still have no idea why it understands me.
		* * 	{not with_six} El: 	What are you?
		* * 	{not with_six} El:		What is your purpose?
		- - 	Head: 	I do not understand the question.
				El:: 	Perhaps it doesn't understand me after all.
	*	[Window - Look]
		She peers out of the window.
		El: 	Still smouldering.
		El:	Looks like I left my mark.

	*	{six_returns} 	[Cabin Door - Enter]
		El:		You coming, Six?
		{ flashback_alberthath:
			Six:	Mistress.
		- else:
			Six: 	It is good to see you recovered, Mistress. I was not sure you would, considering.
			-> flashback_alberthath -> 
		}
		-> goto(FLIGHTDECK) -> flight_deck 

	*	-> six_returns

	-	(done) ->->


= NPC 
	{ 
		- done_some_things() && not six_returns: -> six_returns
		- six_returns && what_happened_down_there <= 2: 			
			-> what_happened_down_there
	}
	->->
	


= what_happened_down_there
	*	El: 	What happened down there?
		Six: 	I will endeavour to remind you, Mistress.
		-> flashback_alberthath -> intro

= six_returns
		~ 	with_six = true
		Six returns to the room, carrying a glass of water.

	- 	Six: Mistress. Some water. 
	-	(opts) 
	
	<- what_happened_down_there

	*	(thanks) El:	 	Thank you.
		She drinks.
		<-	opts
		->->
		
	*	{thanks} El: 	This tastes awful.
		El:		Have you been using the reclaimed water tank again?
		-> suggest
	-	Six:	I have been forced used to broach the reclaimed water tank, Mistress.
	-	(suggest) Six:	I suggest we return to Elboreth soon.
	*	El:		Were we finished here?
		Six:	I see the hopper's effect on your short-term memory has been particularly acute this time.
		<- what_happened_down_there
	*	El:		How long is the return journey?
		Six:	We should be there by evening if we leave immediately.
	- 	->->


/*-------------------------------------------------


	Flashback: Running!


-------------------------------------------------*/

===  flashback_alberthath ===
// tunnel
= enter 
	// don't run scene system at all here...!
	-> running_down_tunnel1 

= running_down_tunnel1
	Screen clears. Flashback begins.

	>>> Three hours earlier.

	-	 El is running down a stone corridor. Six is nowhere to be seen.

		*	El: 	Just ...
		* *		El:		... keep ...
		* * *		El: 	... running ...

	- -> set_path_to(PASSAGEWAY_FORK) -> arrive -> running_down_tunnel2

= running_down_tunnel2
	- El:		Left or right?
		~ temp chosen_route = ""
	- (fork)
		*	(lookleft) [Marked Wall - Look]
			El: 	There's a mark on the wall there. Did I see that earlier?
			-> fork

		* 	[Left Path - Run]
			{ lookleft: 
				El:		Well, here's hoping.
			}
			~ chosen_route = PASSAGEWAY_LEFT_JOIN
			
		*	[Right Path - Run]
			~ chosen_route = PASSAGEWAY_RIGHT_JOIN
	
	-	-> set_path_to(chosen_route) -> chatwhilerun -> running_down_tunnel3
	
= chatwhilerun
		*	El:	Hope ... 
		* *		El: ... and ... 
		* * * 		El:	... pray ...
		*	El:	Here ...
		* * 	El: ... goes ...
		* * *		El: ... nothing ...
	-  // now stop talking, and wait for you to arrive at the next waypoint...
		-> arrive ->->

= running_down_tunnel3
	
	El looks back over her shoulder. Whatever is coming is still coming.

		*	El:		Light!
		*	El:		Six!

	- (opts)
		*	[Passageway - Look Back]
			The passage behind is willing with a wall of sand...
			El:	Keep running...
			-> opts 

		*	El:	Six! Six, are you there?
			Six:	(offstage)  	Here, Mistress!
			-> opts

		*	[Opening - Run]
			-> set_path_to(OUTSIDE_CAVE) -> 
			* * El:	No time to waste.
			* * * El:	This place will fall apart any moment.
					-> arrive -> outside
		
	-	(outside) El explodes from the cavern mouth, almost tripping over a large metal box on the ground.

	-	El:		Six! 
	*	El:		... Where are you?
	*	El:		... Hide, quick! 
	* 	El:		... Get us out of here!

	
	-	Six unfolds - he is the box. Finally, the head materialises.
	-	Six: 	Mistress, is there danger?
	*	El:	 Look at me! What do you think?

	*	(sides) [Side of Opening - Hide]
		El: 	Get out of the doorway.
		She presses herself against the wall.
	
	*	(dunes)[Dune - Run]
		El: 	This way, quick.
		// start El moving, but keep the flow doing this
		-> set_path_to(RUNNING_DOWN_DUNE) ->

	-	(talking) Six: 	Your heart-rate appears elevated, Mistress.
		Six: 	What happened?
		*	El:		I touched something.
		*	El:		Tell you later. 
		*	{dunes} El:	Just keep running.
		*	{sides} El:	Just stay in cover.

	-	Sixes head swivels around...
	
	-	Six: 	Mistress, what is that noise?
	
	- 	A dark wave erupts from the cavern mouth...

	-	...and everything goes black.
		// no need to end scene because it wasn't a scene
		->->




/*-------------------------------------------------
	.... and back to the main game ...
-------------------------------------------------*/



/*-------------------------------------------------


	Location: FLIGHT DECK of the ship


-------------------------------------------------*/

=== flight_deck === 
= enter
	-> set_events(->NPC) -> set_hub(-> hub) ->->


= NPC
	{
		-	not good_working_order : -> good_working_order
	}	
	->->

	- (good_working_order)
		Six: 	The ship appears to be in good working order, Mistress.
		*	El: 	Glad to hear it. 
			Six:	She should fly quite safely. 
		*	El: 	Not affected by whatever was going on below?
			Six:	Not in any way I am aware of, no.
		- 	->->

	- (done) 
		->->


= hub 
	*	(look) [ Console - Look ]
		El:	This is the best ship I could afford when I left Iox, and it's done me well enough so far.

	*	[ Starfield - Look ]
		El::	We're not so far from Elboreth here.
		Six:	Can you pick out home, Mistress?
		* * 	El: 	From here? No.
		* * 	El:		Can you?
			 	Six:	I can. That one.
			 	Six points.
			 	- - - (opts)
			 	* * * 	{not considering} El: 	Which one?
			 	* * * 	El: 	Maybe you'd better navigate.
			 			- - - - (navcomp) 	Six:	I am sure the navigation computer will locate Elboreth successfully.
			 					->->
			 	* * * 	{considering} El:		I'll find my way by feel.
			 			Six: 	Indeed, Mistress.
			 			-> navcomp
		- - 	(considering) Six: 	Considering the viewport as a normalised grid with upper-left origin, Elboreth lies at coordinates...
			 	Six: 	It's somewhere to our left, Mistress.
			 	-> opts

	*	{look} [ Console - Sit]
		El:		All right, Six. Let's go home.
		Six: 	Very good, Mistress.

		// do some flying, to take us to the hub planet of Elboreth
	- ->->

