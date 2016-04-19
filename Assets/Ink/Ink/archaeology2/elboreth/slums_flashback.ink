

VAR 	BY_ALLEYMOUTH = "ByTheAlley"
VAR 	JUNKYARD = "TheJunkyard"
VAR 	LOCATOR_INSIDE_UNDERGROUND_TEMPLE_FLASHBACK = "InsideUndergroundTempleFlashback"


=== function flashback_elboreth_available() ===
	~ return (not under_elboreth_flashback)

=== flashback_elboreth_scene ===
	{
		- not flashback_girl_1: -> flashback_girl_1 -> begin_scene ->->
		- not second_flashback_area: -> flashback_girl_1.shortcut -> begin_scene ->->
		- not under_elboreth_flashback:  -> under_elboreth_flashback -> begin_scene ->->
	}
	->->


/*------------------------------------------------------------------------

	The alley

------------------------------------------------------------------------*/


=== flashback_girl_1 === 

	-> set_events(-> time) -> goto(BY_ALLEYMOUTH) -> 

	>>> I made my first discovery aged twelve, in the slum district of Ghw on the outskirts of Elbroreth.

	§ Note this scene has no Six, and a younger version of El so let's keep it simple; even first-person..?

	§ Setting: an alley between rising slum blocks. El is on the run, having slipped out of the factory where she works. She is being chased by her foreman, a slightly older child named Chi. 

	§The route is fairly linear, but turns corners, so visiblity is a little cut-off. Not being able to go backwards is not a problem, and is highly preferred, so forward-only cameras might work well.
	
	 -> set_hub(->hub) ->->

	


= time
	{ once:
		- 	Chi: 	I know you're in here somewhere! I can smell you!
			El:		I don't think he saw where I went. 
			El:		I don't think he did.
		- 	El:		Don't think they did.
		- 	El:		Have to stay quiet.
		-	
		- 	Chi:	Where are you? 
			El:		Like I'm going to answer that.
		-	Chi:	You don't think you can hide from us, do you?
		-	Chi:	There's a lot of us. A lot more than there are of you!	
		-	
		- 	{bosh(): 
				Chi:	Come out now. We'll be nice.
			- else:
				Chi:	What was that?
				Chi:	Sounded like it came from over there.
			}
		-	Chi: There you are! Now let's see what you're made of...
			-> beaten_up
	}
	->->
	
= beaten_up	

	>>> It could have gone that way. I could have been caught, dragged back to the factory-floor.
	>>> I wasn't. But maybe it's a story for another day.
	-> end_scene ->->

= hub
// the goal is to keep the player pushing forwards along the alleyway
	*	[Alleymouth - Look]
		El:		It isn't even an alley. It's just a gap.
		El:		Did Chi see me?

	*	[Rubbish Pile - Hide]
		El:		I could scramble under there, I suppose.
		El:		Or not. It'd be the first thing they'd kick over, then I'd be stuck.

	*	[Fire Ladder - Climb]
		>>>	I jumped, but couldn't reach the bottom rung of the ladder.
		El:		I can't do it!
		El:		Even if I could, whoever lived at the window would call me a thief and throw me back down.

	*	{not doorway} [Broken Pipe - Grab]
		El:		I could take that and smash him over the head with it.
		El:		Right. 
		El:		Or they'd take it from me, and smash through my skull instead.

	*	(seedoor)  [ Doorway - Look]
		El:	There's a doorway out of here!

	*	(doorway) {seedoor} [Doorway - Open]
		El:		If I could get in and go through to one of the bigger streets...
		El:		It's locked.

	*	(brokenpipe) {doorway} [Broken Pipe - Get]
		El:		Maybe the pipe would be enough to batter the door down with it...
		El:		Oh, Mama, it's heavy.

	*	{brokenpipe && not droppipe} [Doorway - Smash with the Pipe]
		El: 	This is a stupid idea. I can barely lift this thing.
		El:		But I'll try.
		El:		Oh, by the Cloud...
		>>> 	The pipe fell to the ground with a loud clang.
		- - (bosh) Chi:	So there you are! Now we've got you.

	*	(droppipe) {brokenpipe} [Alley - Throw the pipe aside]

		El:		I'm wasting my time carrying this thing.
		El:		I should just run.
		>>> 	I threw the pipe to the ground.
				-> bosh

	*	{seewall} [Low Wall - Look]
		El:		The alley ends up ahead. I'm trapped. That's it. 
		El:		I'm dead.

	*	(seewall) [Low Wall - Climb]
		El:		No choice, Aqila. You can do it. 
		>>> 	I don't how I climbed that wall, but I did. 
		
		-> goto(JUNKYARD) -> second_flashback_area

	-	->->

= shortcut
	-> set_events(-> ranFromFactory) -> teleport(JUNKYARD) -> second_flashback_area

	-	(ranFromFactory)	
		{once:
			-	>>> 	I ran from the factory on Elbroreth, and threw myself over a wall to escape my pursuers. 
		}
		->->


/*------------------------------------------------------------------------
	The walled-up junkyard
------------------------------------------------------------------------*/


=== second_flashback_area ===
= enter
	-> set_events(-> time2) -> set_hub (-> hub2) -> dropped_in ->->

= dropped_in
	>>>		I didn't check the drop on the other side before going over.
	>>> 	I dropped into an area between slum-buildings, forgotten but for piles of tossed garbage. 
	>>> 	There was no one there.


	§ A gap between slums buildings, walled on all sides. It is strewn with rubbish, but not as much as you might expect. Some has been flung over the walls, some has fallen here from passing ships over the years. But largely, this is a gap between things, and there is even bare rock and dust visible in places. 
	->->

= time2 
	{ once:
		- 	El:	It's getting cold. 
		-	
		-	El:	I've got to get home.
		
	}
	->->

= hub2
	*	(walls) [Walls - Look]
		El:	Walled in on all sides.
		El:	They're higher on this side, too.
		El:	Oh, Aqila. What have you done?

	*	[Alley - Listen]
		>>> I listened for the sound of Chi and the others. Nothing.
		>>> They had probably never even found the alley. I had clambered in here for nothing.

	*	(climbing) {walls} [Wall - Climb]
		El:	It's twice as high this side.
		>>> I tried to climb, but got nowhere.


	*	(junk) [Junk - Search]
		El:	Piles of junk.
		{ 
			- climbing: El: If this was a story I'd find a ladder.
			- time2: El: Maybe there's something warm in here. 
		}
		>>> I dug through piles of waste, discarded sheets and bits, but found nothing.
		>>> And then I found something that caught my attention. A sword.

	*	(takesword) {junk} [Sword - Take]
		§ She draws the sword from the junk.
		El:		What's this doing out here? Who would throw this out?
		* * 	(value) El:		It must be valuable.
				El:		To someone.
		* * 	El:		No one would chase me with this.
				El:		If I knew how to use it.
		* *		(beautiful) El:		It's beautiful.
		- - 	El:		There are symbols on the blade.
				>>> 	If only I could remember them now. Maybe I'd know what they said now.
	
	*	(cont) [ Container Crate - Examine]
		El:		Sturdy enough, but empty.

	*	(pryfree) {cont } {not takesword} [Container Crate - Move]
		El: 	If I could move this thing to the wall and climb up...
		El:		Come on.
		El:		It's stuck in the all the... stuff.

	*	(pried) {pryfree && takesword} [ Container Crate - Pry Free]
		{ 
			- beautiful || value: 		El:  I hope this doesn't damage it.
			- else: 					El:  I'd better not blunt it.
		}
		>>> I dug at the base of the crate with the blade again, and again, with more force than the job needed.
		>>> I imagined it was slicing between each one of Chi's ribs in turn.
		El:	There. That might have come free.

	*	(fallen) {cont}	{pried} [Container Crate - Move]
		>>> I heaved the crate over towards one of the walls, and scrambled up.
		El:		I'll have to jump.
		>>> I jumped, and the crate keeled over, and I fell painfully to the ground, hitting my knee on a rock.

	*	(syms) {fallen} [Rock - Examine]
		El: 	Blood. By the Belt! Can this day get any worse?	
		El:		More symbols. What is this place? Why isn't somebody living on it?
		El:		There's houses on every other square inch of this rock.
	
	*	(findring) {syms} [Symbols - Examine]
		El:		They're beautiful. Laid out in a kind of ring.
		El:		Maybe there are more.
		>>> I searched in the junk, forgetting about the crate and wall. Lost in my curiosity.
		>>> And I found more. I found a whole ring of them. Broken apart, some rolled over, but all once part of a ring.

	*	{findring} [Ring of Stones - Connect]
		El:		They were meant to fit together. Like this.
		>>> I was sitting in the centre of the circle when I pushed the last block into place.
		>>> You can imagine my surprise, when everything went dark...

		-> end_scene ->->
		
	- ->->


/*------------------------------------------------------------------------
	
	Below the Slums

------------------------------------------------------------------------*/

===  under_elboreth_flashback ===
= enter 

	>>> 	The skies of the Nebula are always lit by the winds. Now I was in darkness like I had never known.

	TODO Aged twelve, El is underneath the cities of Elboreth in a wide space. Here she finds her pendant and meets a 'ghost'.
	// this is El's great secret, because she [now] knows if it was revealed to the Expeditions Companies they would demolish the slums to extract what lies below. Of course, she didn't know that when she was younger ... so who did she tell? Because of course the company's going to turn up and do just that, or at least get close to it, at some point in the story.

	
	-> set_hub(-> hub) -> set_events( -> talktoself) -> teleport(LOCATOR_INSIDE_UNDERGROUND_TEMPLE_FLASHBACK) -> intro  ->->
	
= intro
	 *		El:		Is there anyone there?
	 		>>> 	Nothing.
	 *		El:		Echo?
	 		>>> 	Nothing.
	 *		El:		Stay calm, El. 
	 		El:		At least Chi won't find you here.
	 -		->->
	 	
= talktoself 
	{ once:
		-	El: 	Just stay calm, El. 
		-	El:		The air down here is stale. 
	}
	->->


= hub 
	 
TODO: Explore underground

	 - 	->->
