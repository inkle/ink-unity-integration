
/* -------------------------------------------------
	External functions and fallbacks
------------------------------------------------- */


EXTERNAL HEADING_FOR(locatorName)
EXTERNAL PAUSE()
EXTERNAL RESUME()
EXTERNAL TELEPORT_TO(locatorName)

=== function HEADING_FOR(locatorName) ===
~ return false

=== function PAUSE ===
~ return

=== function RESUME ===
~ return

=== function TELEPORT_TO(locatorName) ===
~ return

/* -------------------------------------------------
	Game setup
------------------------------------------------- */

// Automatically gets called from game before a scene is run
=== game_setup ===
	LONG_DISTANCE_VERBS look, walk, walk to, follow, throw at, shoot, photograph, scan, go back, go inside
	-> END

/* -------------------------------------------------
	Core walk/talk system
------------------------------------------------- */


VAR _do_block = -> none 
VAR _talk_block = -> none

VAR __scene_running = false
VAR __moving = false


=== _act ===
	{not __scene_running:
		->->
	}
	<- _first_step  		// allow player to start walking
	-> _do_block -> _act  	// or else offer an action hub

=== none ===
	+ -> 
	-	//...picked none
		->->

=== _first_step ===
	+	[MOVE]
		BEAT
		~ __moving = true
		// Enter the movement tunnel, which we leave when we arrive
		-> _move -> _act

=== _step  ===
	+	[MOVE]
		-> _speak -> 
		{ not __moving:
			->-> 			// if we arrived mid-conversation, exit without further movement
		}
		-> _move 


=== _speak === // in a tunnel now
	<- none  // this line ; the default actually gets picked
	{ _speak mod 2 == 1:
		-> _talk_block ->->
	}
	-> DONE

=== _move ===
	<- _step 
	<- arrive 



/* -------------------------------------------------
	Movement hooks
------------------------------------------------- */

=== set_path_to(locator_name)
// moves El without running any systems
	+	[ {locator_name} - DEPART]
		~ __moving = true
		->->

=== goto(locator_name) ===
// moves El using tick system
	-> set_path_to(locator_name) -> _move ->->

=== teleport(locator_name) ===
	~ TELEPORT_TO(locator_name)
	->->


/* -------------------------------------------------
	Pacing hooks
------------------------------------------------- */

=== arrive ===
// insert into side conversations to allow us to arrive
	+ 	[ARRIVE] 
		~ __moving = false
		->-> 

/* -------------------------------------------------
	Scene settings hooks
------------------------------------------------- */

=== begin_scene ===
	~ __scene_running = true
	// offer actions and get on with the game
	-> _act ->
	// set scene running to true again, because we might be running a scene inside a scene, and the end of the inner almost certainly doesn't meant the outer scene has stopped running. 
	~ __scene_running = true
	->->


=== end_scene ===
	~ __scene_running = false
	->->

=== set_hub(-> x) ===
	~ _do_block = x
	->->

=== set_events(-> x) ===
	~ _talk_block = x
	->->

/* -------------------------------------------------
	Standard Dialogue
------------------------------------------------- */

=== done_talking ===
	+	El:		(Say nothing) 
		->->

