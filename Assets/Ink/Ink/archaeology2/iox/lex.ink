
/*

	A Flashback...

	Professor Lex Myari removes El's robot Tallin, modded to remove its Ethical Core, and replaces with another, whcih she stubbornly calls "Two"
	It is implied Two is going to be repossessed after a year of work and its memory given over to shady figures at the university
	Lex suggests she considers destroying the robot, but this isn't explicit

	Mention of "Loop Hypothesis", a semi-religious belief that the Nebula is a site of eternal development and decay, and that ruins are the remains of previous civilisations within this fish-bowl world. El's view of "things beings caused by things" is considered some heretical 

*/

=== flashback_talking_to_lex === 


= top

A map of the Nebula. El speaks in v/o, mirroring the intro to the game.

	-> game_intro_voiceover.introlines1 ->
	-> game_intro_voiceover.introlines3 ->

/*	Done as a tunnel so tweaks carry across..!
	El: 	The Nebula. 
	El:		A crowded pool of worlds, bound together by the swirling dust clouds of the ether.

	El:		But once, the Nebula was joined by single Empire, extending from Iox downwind to Elborath and beyond. 
	El:		Once, great ships sailed as far as the Outer Reach. 
	El:		There are tiny stars in the sky there, that shine with an unnatural light, too far out to sail. 
*/

Camera pulls back.
It is daytime. Aqila and Lex are in the university room on Iox. Lex sits behind a desk, with a few scattered papers. A window overlooks the university grounds. 


Lex: 	So you say, Aqila, my friend. So you claim. 
Lex:	But where's your proof?
-	El:		It's out there. 
	* 	El:	... 	Buried under the dust of a thousand years. 
	*	El:	... 	I've found so much already.
		-> fragments
-	El:	But I'll find it. 
-	Lex:	I don't know what worries me more - that you'll die trying, or that you'll actually find something. 
	*	El: Help me find it. 
	*	El:		I've found plenty already.
		- - (fragments) Lex: 	You've found fragments and hints, nothing more. 
		Lex: 	But then, Aqila... Have you considered if that might be for the best?
		* * 	El: 	Keeps my in work, if that's what you mean. 
				Lex: 	That's not what I mean, no.
		* * 	El: 	What are you saying?
		* * 	El: 	Don't you want to know?

-	Lex: 	The Nebula is a complex place already. The last thing we need is ancient super-powerful technology.
	*	El:	Who said anything about technology?
		Lex:	Who do you think? Bear that in mind, Aqi. 
	*	El: 	I'm looking for answers, not weapons.
		Lex: 	You might be, certainly. But not everyone is you.
- 	Lex: Once you find something, it can't be unfound. 

El:		I'm sure of it. I know. You've walked the ruined streets of the fields of Arkiz. 
El:		I tossed you a stone across the energy bridge between asteroids in Metika.
Lex: 	I remember. But these things have lots of explanations.
*	El:		Ruins have only one explanation.
*	El: 	Lex? Don't tell me you've become a believer?
-	El: 	The Loopers are mad, and you know it.
	Lex: 	Are they? It's as good an explanation as any. Not everyone believes things have a beginning.
*	El: 	You can believe what you like, Lex, but let me look.
* 	El:		This is about funding.
-	Lex: 	We're a serious institution, Aqila. We can't been seen to support wild hypotheses. 
	El: 	Are you saying
	*	(fired) El: ... I'm fired?
		Lex: 	You ready to give up?
		* *		El: 	If I say yes, do I get a raise?
				Lex: 	That's my girl. No, no raise.
		* * 	El:		Sounds like you want me to.
				- - - (dontpretend) 	Lex: 	Don't pretend it's that simple, it isn't. I've done your job.
				-> builtthem
	*	El: ... you believe in the [Loop Hypothesis?] {Loop_Hypothesis()}?
		El:		It doesn't make any sense.
		-> dontpretend
	*	El:	... I should go and find some proof?
- 	Lex: 	You're on your own. But you're right - I have walked the streets in Arkiz.
- (builtthem) 	Lex: 	And I want to know who built them as much as you.
Lex: 	Wait here a moment. 
	>>> Lex headed for the door. 

~ scene(-> NPC, -> hub)

= NPC 
	{
		- not top.fired && not getting_fired: 	-> getting_fired
		- top && TURNS_SINCE(-> top) >= 3: -> lex_returns
	} 
	*	{false} DONE

= getting_fired
	El:		I suppose she's gone to get my firing papers.
	El:		I wonder if I'll be able to keep the boat.
	-> continue

= hub
	*	[Books - Look]
		El:		Professor Myari has the most extensive library on Iox.
		El:		It's mostly nonsense, though. Crazy ideas, like everyone who goes out into Nebula comes back broken.

	*	[Pile of Papers - Look]
		El: 	I wonder where she's gone. 
		El:		Has she left me here to see something I shouldn't?

	*	(papers) [Pile of Papers - Rifle]
		El:		Lex doesn't have bureaucratic accidents, and she's crafty enough.
		~ lower(honour)
		El: 	Let's see what we've got.
	>>>		I began sifting through gently, but then heard a noise. Footsteps, right outside. 
	>>> 	There was only a moment.
		* * 	[Paper - Grab]
				~ paper_about_el = true
		* * 	[Chair - Sit]
		- -		-> lex_returns


	*	[Window - Look Out]
		El:		Iox.
		El:		It wasn't so bad living here, I suppose. 
		* *		El:		... If you like living in a fish-tank.
				El:		Every action catalogued and recorded.
		* * 	El:		...	Even if it is basically a police-state.
				El: 	Ah, Aqila. Are you digging up ruins because you're looking for somewhere better to live?
		* * 	El: 	...	There's a few people I should look up.
				El: 	Assuming they'd be happy to be seen with me.

	*	-> lex_returns

	- 	-> continue


=== lex_returns ===
= top 
	
>>> The door opened and Lex came back in. 
	
	- 	Lex: 	Aqila?  
	{not paper_about_el: 
		-> gotsomething
	}
	-	Lex: 	What are you doing at my desk?
		*	El: 	Looking through your papers. 
				{raise(honour)}
		*	El: 	Wondering about promotion.
				{lower(honour)}
		*	El: 	Archaeology.

	-	Lex: I don't understand you, Aqila.
	- 	-> gotsomething

= gotsomething
	-		Lex:	I've got something for you.
	-		Lex:	This is your new research drone.
	-	(gotrobot)

		*	El:	I've got a robot. 
			Lex: 	Five minutes ago, no, you did not. The university have repossessed your last one.

		*	El: 	What have you done with my robot?
			Lex: 	I knew you'd argue, but look here...

	-
		*	(want) El: 	I want Tallin back. 
		*	El:  	He won't tell you anything, you know.
		 	Lex: 	Oh, we know that. 
		*	El:		You can't just take my robot!
			Lex: 	It was only ever a loan, and not one you took good care of.
	
	-	Lex: 	We {want:also} know you circumvented Tallin's Ethical Core, though we're still not sure how you did that. It's not supposed to be possible.
	
	- (argument)	
		* 	{argument == 1} 	El: 	Yes, and do you know how long it took me to figure out? 
				-> infurating
		* 	{argument == 1} 	El: 	I hate to think what you'd do with the information if you knew.
			- - (infurating) 	Lex:	You're infuriating, Aqila. I should have you thrown out on the spot. These robots aren't free, you know.
				-> argument
		* 	{came_from(->infurating)} 	El:	Then give me a research assistant, like everyone else gets. 
			Lex:	Not a chance. The last thing we need is two of you running around the cloud looking for giants.
			-> argument 

		* 	El:		Lex, you can't do this. I love that robot. 	
			Lex: 	You'll come to love this one too, I'm sure. 
			-> argument

		*	{argument > 1} El:		So who is this thing spying for?
			Lex: 	This robot has an Ethical Core bonded to you, like any other robot. Unlike your last one, in fact.

		*	-> look 

	- (look)
		Lex: 	Look. No choice, all right? This is the arrangement. 
		Lex: 	You'll like this one, anyway. It's an upgrade. 
		*	El: 	You don't understand. Tallin was special. 
			Lex: 	Robots are not supposed to be special, they are supposed to be useful.
			-> grateful
		*	El:		I don't want it. 
			- - (grateful) Lex: 	Be grateful you're getting anything.
		*	El:		I'll lose it first chance I get. 
			Lex: 	Please don't.

	- 	Lex: 	Now, go on. Bond it to you. Give it a name, if you like. 
		*	El: 	Fine. 
		*	El: 	Seriously, Lex. Where is Tallin?
			Lex: 	Honestly? I don't know. 
			Lex: 	But I'll imagine the same people will want to upgrade this one in a year's time, if it's still intact.
			* * 	El: 	Is my work that important?
					Lex: 	(flatly) I don't know what you're talking about, Aqila. 
					Lex: 	Of course your work is important to the university.
			
			* * 	El: 	In a year, you say?
					Lex: 	More or less to the day, I should imagine.

			- -		El: All right, Lex. I'll take it. Just for you.

	- 	El: But I'm not giving it a name. 
		El:	I'm calling it Two.
	
	->->

	



