
VAR SLAVE_MARKET_CENTER = "SlaveMarketCenter"
VAR AMAULD_SHOP = "InsideAmauldsShop"

=== on_fenris_scene === 
	
	-> set_events(-> intro) -> goto(SLAVE_MARKET_CENTER) -> the_slave_market  -> begin_scene -> END

= intro 
	
	- (opts)
	*	El:		We've not been to Fenris before, have we?
	*	El:		A word of warning, Six.
		El:		This place isn't very nice.  It's not very nice at all. 
		Six:	In what way?
		- - (subopts)
		<- done_talking
		* * 	(keep) El:		Just keep your eyes to yourself.
				Six:	I cannot do that, Mistress. 
				-> subopts
		* * 	{not keep} El:		They sell things here that other people wouldn't sell.
				Six:	Mistress?
				-> subopts
		* * 	{ subopts > 1 } El:		Don't say I didn't warn you.
				Six:	Mistress.
		* * 	{ subopts > 1 } El:		I'm afraid your Ethical Core is in for a beating.
				Six: 	That cannot be helped, Mistress. 
				->->

	*	El:	 	This way. 
		Six:	Mistress. 	
		->->

	- 	-> opts
		

=== the_slave_market === 
	
= enter 
	An open market place. Several men and women are loitering under canopies, shaded from the sun. A large woman strides back and forth, calling out prices.

	PROPLIST Low Door, Slaves, Trader

	-> set_events(-> NPC) -> set_hub(->hub) ->->

= intro 
	
	Six: 	Mistress? What is this place?
	*	El:	 	A slave market.
		Six: 	Slaves? 
	*	(donttell) El: 	Don't worry about it, we aren't staying.
		Six: 	Yes, Mistress.
		-> done


	*	El: 	Can't you tell?
		- -  (isaslavemarket) Six: 	This is a slave market. 
	- (react)	
	*	El:		Just keep your head down, Six. 
	* 	El: 	Are you surprised?
		El: 	This is Fenris. It's about as far from Iox as you can go.
	*	El:		You must have known, Six.
		Six:	Yes, Mistress. I...
		Six: 	But knowing something and witnessing it have different effects on my programming.
		* * 	El:		Just ignore your programming and let's keep moving.
				Six:	I cannot ignore it, Mistress.
				Six:	But I will attempt to move on. 	
				->->
		* * 	El:		That's true for everyone.
				Six:	Have you been here before, Mistress?
				* * * 	El:		Let's not talk about it, Six.
						-> letsjustkeepmoving
				* * * 	El:		Yes.
				* * * 	El:		Does it matter?
						~ raise(tension)
				- - - 	(comeon) El:		Now come on.
								-> done
	-	Six:	I cannot be here, Mistress. 
	*	El:		We won't be staying long.
				-> comeon
	*	El:		They buy and sell people, Six, not machines.
		Six: 	I am not concerned for myself.
		-> againstsuffer
	*	El:		What's the problem?
		- - (againstsuffer) Six:	My Ethical Core is programmed against human suffering. It does not differentiate by economic status.
		* * 	El:		Then your ethical core is broken.
				Six:	I fear, Mistress, it is the only one I have.
		* * 	El:		Programmed by optimists.
				~ lower(honour)
				El: 	{down(tension):I'm sorry for you|You're a hopeless case, Six}.
		* * 	El:		There's nothing you can do, Six.
				Six:	I fear I am compelled to spend a considerable number of cycles verifying that before I can accept it, Mistress.
		- - 	
		* * 	(letsjustkeepmoving) El:		Let's just keep moving and try not attract too much attention.
		* * 	El:		I'm sorry, Six. 
				El:		I wish I could fix it for you, I really do.
				~ lower(tension)
	- (done) ->->


= NPC 
	{ not intro: -> intro }
	{ hub.slaves || hub.trader:
		- intro.donttell && not intro.react: 
			Six: 	Mistress?
			-> intro.isaslavemarket
	}
	{ intro.done:
		{ 

			-	not well_treated: 	-> well_treated
			-	not well_treated.whynotfight: -> well_treated.whynotfight
			-	not well_treated.onedaychangeopts: 	
					Six:	Will this trade end one day, Mistress?
					-> well_treated.onedaychangeopts
		}
	}
	{
		- not why_here: -> why_here
	}
	- ->->
	- (why_here) Six: What is our purpose here, Mistress?
		{ intro.done: Six: We are not purchasing, I hope. }
		* 	El: 	There's someone here I'd like you to meet.
		*	El:		You'll see.
		*	El:		Just stay quiet, won't you?
			~ raise(tension)
			{ not trader_blocks_way: -> trader_blocks_way }
	- ->->
		
	

= well_treated 
	Six: 	Do you believe they are well-treated, Mistress?
	*	El:	I'm sure they are.
		~ lower(tension)
		Six:	You are lying, Mistress, but I appreciate your consideration.
		* * 	El:		It's not for your benefit, Six.
				El:		My own Ethical Core doesn't do so well with this place either.
		* * 	(onedaychange) El:		One day, this will change, Six. It has to.
				<- arrive
				* * * 	[MOVE]
						Six:	How?
				- - - 	(onedaychangeopts)
				* * * 	El:		I don't know.
						El:		But I feel it has to.
						Six:	I cannot be certain, Mistress. 
				* * * 	(believe) El:		You have to believe it. 
						Six: 	I cannot believe things, Mistress. I can only compute probabilities. But here...
				* * *	El:		Slaves always overthrow their masters in the end.
						El: 	They never stop trying.
						Six:	No.
				- - - 	Six: 	The error margins are too large. There is not enough data.
						<- arrive
						* * * *	{believe}	El: 	That's when you have to believe.
						* * * * 	El:		There is only one fact that matters.
									El:		No one wants this place to exist. Not even those who profit from it.
						* * * * 	El:		Nothing stays the same, Six. Nothing.
									-> arrive	

 	*	El:	Some will be.
 		El:	Not all. 
 		- - 	(whynotfight) Six: 	Why do they not fight back?
 		* * 	El:		There's only so far you can run when you've got a chain around your ankles.
 		* *		El:		Some do. I doubt they get far. 
 		- -		Six: 	And how do the traders function?
 				Six: 	How are they able to do this?
 		* *		El:		Six, just leave it.
 				El: 	You can't fix a broken planet, or out-think it. 
 		* * 	El:		I don't know, Six. 
 				El: 	But it's not an oversight. They know what they're doing.
 		* * 	El:		The money. They trade others powerlessness for their own power.
 	
 	-	->->


= hub 

	*	(slaves) [Slaves - Look]
		El:		It turns my stomach.

	*	(trader) [Trader - Look]
	>>> The trader slumped on her chair with a fan, eyeing the crowd like a hungry bird.

	*	(low_door) [Low Door - Look]
		El: 	There. That's where Amauld lives. 
		Six:	Very good, Mistress.

	+	{low_door} [Low Door - Walk]
		El:		{In here.|I've had enough of this place.}
		{ not trader_blocks_way: ->trader_blocks_way }
		
		-> goto(AMAULD_SHOP) -> amauld_shop 
		


	- ->->


=== function angered_slave_trader() ===
	~ return ( trader_blocks_way.hard1 + trader_blocks_way.hard2 + trader_blocks_way.hard3) >= 2

=== trader_blocks_way === 
= top
	>>> The trader roused herself, stepping down from her stand to block our path.
	Trader:	Is that a robot?
		* 	(hard1) El:		Get out of my way. 
		* 	El:		You've got keen eyes, trader.
		* 	El:		It's not for sale.
			Trader:	Ha!
			Trader: I wouldn't take your robot if you gave it to me for nothing. Why would I?
			* *	El:		Happy to treat people like machines instead?
				Trader: I'm just another employer. While these things... 
				-> pokes					
			* *	El:		Then we have nothing to discuss.
			* * 	El:		Is there something you want?
	- 	Trader: 	Who are you to bring a mechanical here?
	- (pokes)
>>>	The trader poked Six in the eye.
	Trader: 	What good are these to anyone?
		* 	(hard2) {not up(tension)} El:		Don't do that again.
		*	{not down(honour)} El:		I'd rather my servant wasn't a person.
			-> yourkind	
		*	{not up(honour)} El:		I don't want argument with you.
			Trader: 	Then get off my planet and take that thing of yours with you.
			* * 	El: 	I've got business here. 
					El:		But believe me, I won't stay a moment longer than necessary.
					Trader: 	Be sure that you do not.
					-> yourkind
			* * 	El:		I'll do what I choose. 
					-> ha
			* *		El:		Peace, trader. We'll be gone soon enough.
					Trader: 	Be sure that you are.
					-> yourkind 	
		* 	El:		What's wrong with you? 
			El: 	Did someone cut out your heart when you were a baby?
	-	(ha) Trader:		Ha! 
		Trader: 	You're fearless, aren't you? 
		Trader: 	I suppose you think that thing of yours will stop anyone from demanding a little more respect.
	* 	El:			The only respect you deserve is my spit in your eye.
		Trader: 	Look, dog. This is my job. My profession. My mother did it, her father before her. 
		Trader: 	It's how I put food on my table.
	*	El:			I don't want any trouble.
		Trader: 	Then watch what you bring onto our planet.
	*	El:			Don't you have a shop to run?
		El:			Or is business slow today?
	-	(yourkind) Trader: Your kind won't be happy until everyone in these clouds is starving and only the machines get to eat.
	* 	El:		If you go out of business, I won't shed a tear.
		El:		And neither will those people you have in chains.	
	* 	(hard3) El: 	It won't be robots that stick a knife in your belly. 
	* 	El:		You don't look underfed to me.
	- 	{angered_slave_trader():
			El: Now get out of my way. 
			Trader: 	I won't forget your face, friend.
		- else:
			El:			Now, please. Let me pass.
			Trader: 	Be quick about your business.
		}
	>>> The trader stepped aside, clambering back to their place.
	-	->->



