
VAR 	IOX_PLAZA = "IoxPlaza"
VAR		LOCATOR_DOME_OF_PHILOSOPHY = "DomeOfPhilosophy"
VAR 	IOX_LIBRARY = "LibraryOfIox"

=== iox_scene ===

	The plaza of the University of Iox, which sprawls across an enormous portion of this planet. It is laid out like a Roman villa; clean, with gardens and fountains. Starmaps adorn the walls and the buildings are domed, decorated with swirl-like representations of the winds.

	-> set_events(-> intro)-> goto(IOX_PLAZA) -> intro -> iox_plaza -> begin_scene


= intro
	{|->->}	
	>>> Iox. Home of the University, the Council, and the wealthy.
	>>> Most Oxers don't ever leave. They wait for the Delta to come to them. 
	->->
	


=== iox_plaza ===
= enter
	-> set_hub( -> hub)  -> set_events(-> NPC) ->->

= NPC
	{
		-	not history: 
				-> history 
		-  	seen_but_not_recently(-> history) && not good_to_be_home: 
				-> good_to_be_home
		- 	scenes_inside_room.message_from_iox_re_alberthath && not professor_meeting: 
				-> professor_meeting
	}
	- ->->
	- (history)
		Six: 	Mistress? Are you all right?
		*	El:		Fine. I'm fine. 
			Six:	I am detecting elevated heart-rate. 
			* * 	El:		I hate this place. 
					Six:	-> people_on_elboreth
			* * 	El:		Then stop detecting them.
					~ raise(tension)
					Six:	I cannot do that, Mistress. 

		*	El:		Look at this place. 
			* * 	El: 	...	There's so much money here. 
				 	- - - (people_on_elboreth) El: 	People on Elboreth just don't know that a life like this is possible. 
				 			Six:	There are a lot more people on Elboreth than here. 
				 	* * * 	El:		There are. 
				 			~ raise(honour)
				 			El:		But even that seems the wrong way up.
				 	* * * 	El:	You're right, of course.
				 			{down(honour):
				 				El:	There's never enough to go around.
				 			- else:
				 				~ raise(honour)
				 				El:	But it doesn't seem right.
				 			}				 			
			* * (beautiful)	El:		... It's beautiful.	
					Six:	
	- ->->
	- (good_to_be_home)
		Six:	Ah. It is good to be home. 
		<- 	done_talking
		*	{not down(tension) } El:		Don't pretend to have feelings. 
			~ raise(tension)
			Six:	You are well of my capacity for feelings, Mistress.
			-> prob
		*	El:	 	{not beautiful} This isn't home.
			Six:	It may not be your home, Mistress. 
			<- done_talking
			* *		El: So you like it here? 
					-> suppose
		*	El:		You feel like that, do you?
			Six:	I suppose I do, Mistress. 
			<- done_talking
			* * 	(suppose) El:		I didn't know you had feelings.
					- - (prob) Six:		I like this place. 
							Six: The probability of harm is particularly low here.
	- 	->->
	

= hub 
	+	[Library - Look]
		>>> The great Library of Iox holds a copy of every book we have. It's still only a single room.

	+	[Library - Enter]
		-> goto(IOX_LIBRARY) -> iox_library

	*	[Dome of Philosophy - Look]
		El:		Professor Myari's office is in there. 


	+	[Dome of Philosophy - Enter]
		El:		Let's go and see if the Professor is in.
		// walk to Professor's room

	-	->-> 

/*--------------------------------------------------------------------

	First meeting with Professor Lex

--------------------------------------------------------------------*/

=== professor_meeting ===
= outside	
	Lex:	Elesira! There you are.
	*	El:		Professor!
	*	El:		She's been waiting for us.
		Six:	That seems likely, Mistress. 
	-	{TURNS_SINCE(-> scenes_inside_room.message_from_iox_re_alberthath) < 20:
			~ raise(lexi)
			Lex:	Thanks for coming so quickly. A safe flight?
		- else:
			~ lower(lexi)
			Lex:	Glad you finally decided to turn up.
			Lex: 	An uneventful flight, I hope?
		}
	*	El:		I didn't blow anything up, if that's what you mean.
		Lex:	That's what I mean. I'm glad to hear it.
	*	El:		Don't treat me like an idiot.
		Lex:	I'll stop treating you like an idiot when you stop acting like one.
		~ lower(lexi)
		* * 	El:		Guilty until proven innocent, I see.
				Lex:	Don't be melodramatic.
		* * 	El:		Professor, what happened on that asteroid wasn't my fault.
				Lex:	You may be right. I hope you are.
	- 	Lex: 	And how are you, Six? Fully functional, I hope?
		Six:	Indeed, Professor. 
		*	El:		I've not done anything to Six.
			Lex:	You'll forgive me if I'm not confident of that.
			Lex:	I remember what happened to Three. 
			Lex:	The damn thing was speaking backwards by the time you finished with it.

		*	El:		Six and I have been getting on well.
			Lex:	So you've not broken it, dropped it down a hole, or scrambled its brain yet?
			Lex:	Watch yourself, Six. This one has a thing against robots.
			Six:	I am most cautious, Professor. 

	- 	Lex:	Follow me.

		>>> She moved off to her office. 

	*	[Dome of Philosophy - Enter]
		El:	 Come on, Six.
		-> goto(LOCATOR_DOME_OF_PHILOSOPHY) -> dome_of_philosophy

		
=== dome_of_philosophy ===
= enter 
	-> set_hub(->hub) -> set_events(->NPC) ->->

= hub 
	->->

= NPC
	TODO: Lex talks to you about blown up asteroid and provides a new dig site.

	-> leave 

= leave 
	-> goto(IOX_PLAZA) -> iox_plaza
	

