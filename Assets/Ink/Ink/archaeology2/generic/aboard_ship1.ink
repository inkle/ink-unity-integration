
=== exit_to_ship ===
	GET THE PLAYER BACK TO THE SHIP SOMEHOW!!
	-> DONE

=== aboard_ship_to_elboreth ===

{ once:
	- -> first
}
->->

= first
	
	>>>	The ship drifted along the winds of the Nebula.

	<- dreamy_space_thoughts

	*	El: 	How long till we reach Elboreth?
		Six: 	Perhaps you would prefer to sleep?
		* * 	El:		I'll stay awake for a while, I think.
				El:		Might as well enjoy the peace and quiet while we have it. 
		* * 	El:		Good idea. 
				Six: 	Shall I prepare your quarters?
				* * * 	El:		I'll sleep here, I think. 
						El:		Under the stars.
				* * * 	El: 	Thank you, Six. 
						El: 	Where would I be without you?
						Six:	Still on the surface of Alberath, Mistress, with your leg trapped under a stone.

	- ->->



=== aboard_ship_to_iox ===

	>>> I sat at the flight deck, boots up on the console.

	{ 
		- up(tension) && not robot_trustworthy: -> robot_trustworthy

	}

	<- dreamy_space_thoughts
	
	-	->->

= robot_trustworthy
	Six:	Mistress, can I bring you any refreshment?
	*	El:		Some water.
		Six:	Mistress.
		~ with_six = false
		>>> 	He rolled away, and the room seemed to expand. But he wouldn't be gone for long.
	*	El:		I don't need anything.
		Six:	Very good, Mistress.
	*	El:		Why don't you go and rest?
		Six:	I do not require it.
	>>> 	Six isn't the first robot they've given me. But for some reason I trust this one less than the last.
	>>> 	Does Six care about me? 
	>>> 	I rely on his Ethical Core to pull me out of doing stupid things all the time.
	>>> 	What if one day Six doesn't pull me out? Am I certain that would never happen?
	*	{with_six}	El:		We've had a few close calls recently.
		Six:	Indeed, Mistress.
		Six:	But all have been within acceptable parameters.
	*	{with_six}	El:		Do you think we're getting close to finding anything out here, Six?
		Six:	We are constantly finding things, Mistress.
		Six:	I am not in a position to judge their value.
	*	-> back
		- - (back)	 ~ with_six = true 
			>>> Six returned with a glass of water. I put it down on the console, untouched.
			Six:	We will need to refill the tank on Iox, Mistress.
	- 
	>>> 	The Professor is right when she says I can't be trusted to look after myself.
	>>> 	But how much does Six tell them? 

	-	->->


=== dreamy_space_thoughts ===

	* 	{dreamy_space_thoughts mod 3 == 0} {silent()}	El:	 There are people in this Nebula who have no idea what's out here.
		El:		People who've never set foot in space.
		Six:	When did you first sail, Mistress?
		* * 	El:		By myself?
				El:		Early twenties, I suppose. I hired a skiff from Iox and took it the nearest belt.
				El:		I was going to find my first great find.
				Six:	Did you?
				* * *	El:		No.
				* * * 	El:		I had to be rescued.
						El:		I drifted off the winds and ran out of fuel.
						Six:	That will not happen in my presence.
						* * * * 	El:		I'm a better sailor now, Six.
						* * * * 	{not down(tension)} El:		No; I'd throw you overboard for thrust.
									~ raise(tension)
						* * * * 	{not up(tension)} 	El:		Thanks, Six, it's nice to know.
				* * * 	El:		What do you think?
						Six:	I will assume the worst, Mistress.
		* * 	El:		The first time? I was young.
				El:		Twelve years old, I think. I took a frigate to Iox.
				Six:	You were running away?
				* * * 	El:		No.
						- - - - (sent) El:		I was sent to the university. They wanted to meet me.
						Six:	For what reason?
						- - - - (opts)
						* * * *		El:		They wanted to give me a job. At twelve.
									El:		I never stood a chance, really.
						* * * *		El:		I'd found something.
									El:		They wanted it, so they gave me a trip through space and asked me to hand it over.
									El:		I didn't.
						* * * * 	{not down(tension)} El:		Don't you know?
									El:		I assumed they'd told you.
									~ raise(tension)
									Six:	Mistress, I am just a robot. 
									-> opts
						* * * *	{opts > 1} 	El:	 Never mind, anyway.
				* * * 	El:		In a way, I suppose so.
						--> sent 
				* * * 	El:		It's ancient history now.
						El:		I don't want to think about it. 
		

	*	{dreamy_space_thoughts mod 3 == 1} {silent()}  El:  	Do you ever look at it, Six? This Nebula of ours.
		Six:	I have seen it often enough.
		* * 	El:		You're dodging the question.
				Six: 	Hardly, Mistress. I do not understand the question.
		* * 	El: 	But what do you make of it?
				El:		What's behind it all?  
				Six: 	I do not think I understand the question, Mistress.
		* * 	El:		What does it hide, do you suppose?
				-> itslarge
		- - 	El: 	{ I mean, | But }
		* * 	El:		... the shape. The structure. All these scattered rocks, planets.
				El:		All thrown together like pots in a bazaar.
				El:		It doesn't seem 
				* * *	El:		... quite right, somehow. 
				* * * 	El:		... good enough, to me. I would expect the creator to do better.
				* * *	El:		... the most elegant of designs for a universe.
				- - - 	Six: 	I do not believe I my opinion counts on the matter, Mistress.
				El:	I suppose
				* * *	El:		... that's true enough.
				* * * 	El:		... it just seems like a strange way for the universe to be.
					 	El:		If I were to believe in a creator, I would want one that was better organised.
				* * *	El:		... you see lines of force, and nothing more than that.
				- - - 	(believeforce) Six:	I understand that the Nebula is organised by basic principles of gravity and ether flow.
				- - - 	(believeopts)
				* * * 	El:		But what is gravity, really? 
						Six:	The attraction of matter to other matter.
				* * * 	El:		But what is ether flow, exactly?
						Six: 	The movement of space within the Cloud, Mistress.
				* * * 	{easy} 	El:		But what if there's more to it? 
						Six: 	I cannot see how. Both gravity and ether are well-understood, Mistress. 
						Six: 	They have no more meaning than the way that water can freeze.
						-> believeopts
				* * * 	El:		Then why is it such a state?
						-> observed
				- - - 	Six: 	But I suspect I have missed your meaning.
				* * * 	El: 	There's a dream I often have... 
						El:		Of a single planet, going around a Sun. 
						* * * *		El:		It's not only me. Others have it.
									Six: 	Does it come from a story?
									* * * * *		El:		I suppose so. I don't know where I first heard it.
									* * * * * 		El:		Every story is a reflection of some need. 
									- - - - -		Six:	The Nebula is a most complex environment, certainly. 
						* * * * 	El:		It's a dream of a tidier place. This one is too messy.

		* * 	(easy) El:		... it's easy to assume that everything has meaning.
				El:		As though all these rocks and stones must have been placed by someone, for a reason.
				Six:	-> believeforce

		* * 	El:		... what do you suppose is out there? 
				El: 	Or have we seen it all?
				- - -	(itslarge) 
						Six: 	The Nebula is large, Mistress, but well connected. I believe most of its secrets have been unearthed. 
				* * *	El: 	You do? Then what's the point in what we do?
				* * *	El:		That's a poor way to live. 
						Six: 	I am a machine, Mistress. My parameters are quite well-defined.
						* * * *		El:		Nothing is ever well-defined, Six.
									El:		Even your previous Ethical Core seems to be constantly open for reinterpretation.
						* * * *		El: 
		- -		(observed) Six: 	I have observed, Mistress, that the complexity always arises from small details. 
				Six: 	Take my construction, for example, Mistress. My basic functions are quite basic. 
				Six: 	But the complexity in moving fluid from one part of my body to another; that requires great tangles of cables.
		* * 	El: 	So you think it's just a matter of minor details?
				El:		Little things, that make the whole Nebula splatter outwards like a dropped box of rice?
				Six: 	It does not seem implausible.
		* * 	El:		
		* * 	{not down(tension)} El:		I'm sorry, Six, you're simply poorly built. 
				{raise(tension)}
				Six:	Whereas I doubt the Nebula even had an architect, Mistress.
				* * * 	El: 	Don't let anyone on Iox hear you say that.
						Six:	Indeed I will not, Mistress.
				* * *	El:		That much is obvious.

	
	
				
	-	->->