VAR LOCATOR_INSIDE_ROOM = "InsideElsRoom"
VAR OUTSIDE_ELS_DOOR = "OutsideElsDoor"

/* -------------------------------------------------
	Piece of Geography - the Apartment
------------------------------------------------- */

VAR six_staying_outside = false

=== apartment_corridor ===
= enter 
	-> set_events(-> generic_Six_conversation) -> set_hub(-> hub) -> at ->
	->->

= at
	§ 	 A long tunnel, lit by a lightcables strung along the walls. Doors lead off here and there.
 	
 	{ 
 		- 	not job_offer && the_blue_monkey && not missed_job_offer: -> missed_job_offer -> 
 		-	not job_offer:	-> job_offer -> 
 	}
 	
 	~ six_staying_outside = false

 	->-> 


 = hub 
 	*	(stayoutside) El:		You stay out here.
 		~ raise(tension)
 		Six: 	Mistress.
 		* * 	El:		Guard the door. 
 				Six:	From what, Mistress?
 				* * * 	El:	Anything you like.
 				* * * 	El: 	
 		* * 	El:	I'm not sleeping with you inside the room.
 		* * 	El: Good night.
 		- - 	~ six_staying_outside = true
 	+	{seen_some_time_ago(-> stayoutside)} El:	You stay here.
 		Six: 	As you wish, Mistress.
 		- - 	~ six_staying_outside = true
 	+	{seen_some_time_ago(-> stayoutside)} El: 	You might as well come in. 
 		{once:
 			- El: 	Don't want anyone walking off with you.
 		}
 		{ cycle:
 			- Six:	Mistress. 
 			- Six:	Thank you, Mistress.
 		}
 		- - 	~ six_staying_outside = false
 				-> enterdoor
 	+ 	[Els Door - Enter]	
 		- - (enterdoor)
 			{six_staying_outside:
 				~ with_six = false 
 				>>> Six took up position outside the door.
 			}
			-> goto(LOCATOR_INSIDE_ROOM) -> inside_room
	- ->->

 	

/* -------------------------------------------------
	Inside your Room
------------------------------------------------- */

=== inside_room === 
= enter
	-> set_hub(->hub) -> set_events( -> generic_Six_conversation) -> intro -> 
	->->

= intro 
	El's room. Potentially nothing to do here, story-wise, might simply be a UI/pacing thing, depending on how the flow works.
	Broadly, this is the end of the tutorial / intro section, and should start opening out a bit

	TODO: some logic for saying "are you shutting down for the night". Perhaps you always are when you come here tho
	
	-> do_night_time -> 
	->->


= hub 

	*	{atlas_card && seen_reasonably_recently(-> job_offer)} El:	This offer from Atlas Expeditions...
		El:	 	Someone's noticed me, at least. 
		Six:	There are not many explorers of these asteroids.
		* * 	El:		Flattering.
				{up(tension):
					Six:	I aim for truthfulness, Mistress, nothing less.
				- else:
					Six:	I apologise for any offence, Mistress.
				}
		* * 	El:		And most work for this lot. 
				Six:	They seem a most substantial operation.
		- - 	
		* *		El:		We'd be well-funded, for once. A good ship.
				El:		Maybe some decent scanners.
				Six:	You might be required to work in a team.
		* *		El:		I wouldn't have the freedom the university gives me.
				Six:	Presumably not, Mistress.
		* * 	El:		I wonder how they make their money?
				Six:	An interesting point. 

	*	{atlas_card} {not called_atlas} 	[Six - Call Atlas Expeditions]
		-> called_atlas ->


	+	[Door - Leave]
		El:		Come on, Six.
		Six:	Mistress.
		
		->goto(OUTSIDE_ELS_DOOR) -> apartment_corridor

	-	->->



/*----------------------------------------------------
		Evening -> Sleep -> Morning
---------------------------------------------------*/

= do_night_time
	{ 
		- flashback_alberthath && not scenes_inside_room.reflect_on_alberthath: -> scenes_inside_room.reflect_on_alberthath -> 
		- flashback_elboreth_available(): 	-> flashback_elboreth_scene -> 


	}
	-> sleep

= sleep 
	§ Do whatever transition we do for night time.
	-> morning 

= morning

	{
		-	scenes_inside_room.reflect_on_alberthath && not scenes_inside_room.message_from_iox_re_alberthath: 
					-> scenes_inside_room.message_from_iox_re_alberthath ->
					->->
		- 	not six_staying_outside:
				Six: 	Good morning, Mistress.
				->->
	}

	
	->->

/* -------------------------------------------------


	Dialogue scenes that can take place here; break up if necessary.


------------------------------------------------- */

=== scenes_inside_room === 

= message_from_iox_re_alberthath
	Six:	 Good morning, Mistress. I have a communication from Professor Myari.
	*	El:		All right.
	*	El:		Give me a moment.
		- - (minihub)

		* * 	[Sink - Wash my face]
				>>> I splashed my face a little.
				-> minihub
		* * 	[Light - Turn on]
				>>> I turned on the lamp.
				-> minihub
		* * 	{minihub > 1} El: All right, then.
		
	-	Six:	Elesira? It's Lexi.
	*	{minihub} El:	Sorry to keep you waiting.
		Six:	Right. 
	*	El:		What can I do for you?
	*	El:		If this is about the asteroid yesterday...
		
	-	Six:	I don't suppose you've launched already this morning?
		* 	{minihub} El:		I've only just woken up.
		* 	El:	No. Should I have?
		* 	El:	What's this about?
	-	Six:	That planetoid you visited yesterday. Your great discovery.
		Six:	What did you do?
		*	El:		We landed, and set off some kind of system.
			El:		The asteroid came apart. We barely escaped.
			Six:	Look, I'm glad you're intact. Both of you.
		*	El:		Nothing. We looked around.
		*	El:		Seriously. What's happened? 
	-	Six:	As of this morning, that asteroid is gone.
		Six:	There isn't even any debris.
		*	El:		Gone?
		*	El:		It was falling to pieces when we left.
			El:		I suppose it must have collapsed. 
			Six:	When things collapse, they don't vanish.
			Six:	Whole asteroids don't vanish.
	-	
		*	El: 	The winds...
			El:		Maybe they carried the pieces away.
			Six:	Most rocks don't have lightsails, do they?
		*	El:		That doesn't make any sense.
			Six:	I couldn't agree more.
		*	El:		Do you want me to go and have a look?
			Six: 	No. 
	-	Six:	I want you to come to Iox. More to the point, I want Six to come.
		Six:	I want to see exactly what you did.
		*	{atlas_card} El:	I have somewhere else I should be.
			Six:	We pay for your apartment, Ms Elassar. Be here.
			~ lower(lexi)
		*	El:		I'll head over once I'm up.
			Six:	I'll be waiting.
		*	El:		I could go and have a look.
			Six:	There's nothing to see.
			Six:	All the interesting material is in your robot. So bring it here, please.
	-	Six:	And Elesira?
		*	El:		Yes?
			Six:	Don't blow up anything else on the way.

		*	El:		Goodbye, Professor.
			~ lower(lexi)
			>>>	I gestured for Six to cut the connection, which he did.

	-	Six: 	Mistress? Will we be heading to Iox?
		*	El:		I suppose so. 
		*	El:		I haven't decided yet.

	-	->->




= reflect_on_alberthath

	>>> I stretched out on the bed, and sighed.
	
	Six:	Will you rest now, Mistress?
	*	El:		We'll have to go back to that little planet, Six.
		Six:	Yes, Mistress, though I fear whatever was there will have been destroyed.
		* * 	El:		What happened?
		* * 	El:		Did we cause it?
		* * 	El:		We didn't destroyed the entire asteroid.
				Six:	No, Mistress, but I believe any structures may have been crushed.
		- - 	(believe) Six:	I believe there was some machinery inside the asteroid. 
				Six:	We may have activated it inadvertently.
		* * 	El:	 	The Professor won't be pleased.
				El:		I'm meant to find sites, not destroy them.
		* * 	El: 	What kind of machinery does that?
		* * 	El:		Well, at least we found something.
		- -		Six:	I will consider what it might have been, Mistress, while I sleep.
	*	El:		In a moment. 
		El:		It's been quite a day.
		Six:	Yes, Mistress. We were somewhat closer to death on this particular occasion.
		* * 	El:		Did we destroy that asteroid, Six?
				-> believe
		* * 	El:		You think I'm close to death when I use a toaster.
				Six:	Mistress, the entire asteroid collapsed on itself, crushing everything within.
	*	El:		Can't you go somewhere else?
	-	Six:	I will power down in the corner.
		Six:	I may make occasional soft bleeps, I hope that will not disturb you.
		*		El:		That's all right, Six.
				--> thanks
		*		El:	 	No more than last night.
				- - (thanks) Six:	Thank you, Mistress.
		*		El:		Do you have to do that?
				Six:	It is a side-effect of my data processing.
				Six:	I fear I am not in control of it. 
	-	
		*		El:		Good night, Six.
				~ lower(tension)
		*		El:		It was probably your bleeps that set off that asteroid.
				~ raise(tension)

	-	>>> I closed my eyes, and tried to sleep.

	- 	->->


