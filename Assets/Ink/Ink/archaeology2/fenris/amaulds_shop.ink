
/*------------------------------------------------------------
	
	Amauld's Shop

------------------------------------------------------------*/

=== function amauld_present() ===
// track this properly
	~ return with_amauld

=== function waiting_for_amauld() ===
	~ return (seen_more_recently_than(-> amauld_shop.hub.ring, -> amauld_comes_and_goes.comes_out))

VAR with_amauld = false
VAR met_amauld = false

=== amauld_shop ===
= enter 
	-> set_events(-> events) -> set_hub(-> hub) ->-> 

= events
	{ with_amauld:
		-> amauld_chat
	}
	{ not with_amauld:
		{
			- not quieter_here: -> quieter_here
			- not what_does_shop_do && not met_amauld: -> what_does_shop_do
		}
	}
	{ waiting_for_amauld():
		-> amauld_comes_and_goes.comes_out
	}
	->->
	- (quieter_here)
		Six:	It is quieter in here.
		*	El:		Don't be fooled.
		* 	El:		We'll be off this rock soon.
	- 	->->
	- (what_does_shop_do)
		Six:	What does the owner of this establishment do?
		*	El:		Six, no one says 'establishment'.
			El:		If you mean shop, say shop.
			
		*	El:	Sells and repairs things. Mechanicals, mostly. 
			
		- 	Six:	Is this a shop? Is this junk for sale?

		* 	El:	It's not junk.
			El:	Not all of it, anyway. 

		* 	El:		It's not a dig site. 
			Six:	A little sand might improve the decor. 
			<- done_talking 
			* *	El: Was that a joke?
				Six:	I do not like this planet, Mistress. 
				Six:	And I do not like this 'shop'.

		*	El:	What did you think we were doing here?
			{ up(tension):
				~ raise(tension)
				Six: You are hardly in the habit of explaining your projects to me, Mistress.
			- else: 
				Six:	I am still gathering data for my hypothesis, Mistress.
			}
	->->

= amauld_chat
	{
		- not glad_to_hear: -> glad_to_hear	
	}
	->->
	- (glad_to_hear) Amauld: Glad to hear you're in one piece, however.
	*	El:			Why wouldn't I be?
	 	Amauld: 	You hear things. Just, things.
	*	El:	Just about. 
		Amauld:		You should be careful out there.
	-	->->

= hub 
	*	{not met_amauld } {not amauld_present()} [Toys - Look]
		El:		Amauld makes all sorts of toys.
		Six:	What are they?
		* * 	El:		Not robots, exactly.
		* * 	El:		Little devices. Toys. 
		* * 	El:		Just trinkets.
		- -		(toy) Toy: 	Are you talking to me?
		* * 	El:		(Say nothing)
				Six:	Obviously she was not.
				-> thingies

		* * 	El:		About you, yes. 
				Toy: 	You shouldn't talk about people like they're things.
				- - - (thingies)
				* * * 	El:		You're not a person.
						Toy:	Come over here and say that.

				* * * 	El:		Where's Amauld?

		* * 	El:		Amauld, come out from behind the curtain.
		- - 	Toy: 	Amauld isn't here. 
				Toy:	Amauld says, go away big nose.

	*	{amauld_present()} [Toys - Look]
		El:		What are you working on?
		Amauld:	Nothing much. These are mostly half-ideas. 
		Toy:	Well. I like that.

	+	(ring) [Bell - Ring]
		{ seen_recently(-> firstring):
			>>> I hit the buzzer again impatiently. From the backroom I heard a curse.
			-> amauld_comes_and_goes.comes_out
			
		}

		- - (firstring)	>>> The bell makes a nasty buzzing sound. There is a clattering from the backroom.
		
	+	{ring} {waiting_for_amauld()} {seen_more_recently_than(-> ring, -> leave)} 
		[Wait]
		>>> 	We waited. 
		-> amauld_comes_and_goes.comes_out

	+	(leave) [Doorway - Exit]
		{ waiting_for_amauld():
			El:	Oh, forget it.
		}
		// say goodbye if appropriate 
		-> set_events(-> amauld_comes_and_goes.goodbye) -> goto(SLAVE_MARKET_CENTER) -> the_slave_market

	- 	->->


=== amauld_comes_and_goes
= comes_out
	{ stopping:
		- -> first -> hub
		- -> repeat -> hub
	}
= first 
	>>> Finally, a man appeared from the back room, wiping his hands on an oil-stained cloth.

	Amauld: What do you want? Oh - it's you.
	~ with_amauld = true 

	>>> Amauld and I are old friends.

	*	El:		Amauld.
	-  	Amauld: Are you here to sell me another broken scanner, like last time?
	* 	El:		I didn't know it was broken.
		>>> 	I did, of course. I was five duckets short on a new sail.
	* 	El:		I don't have anything to sell you.
		Amauld: No. No, you don't.
	*	El:		I don't remember that.
		Amauld: Oh, don't you? Ripping people off just doesn't stick?
	- >>> We're old friends, after a fashion.
		Amauld: What do you want?

	->->

= repeat 
	>>> Amauld finally emerged from the back room.
	{ seen_reasonably_recently(-> goodbye):
		Amauld: Elesira. Did you forget something?
	- else: 
		Amauld: Elesira. What can I do for you?
	}
	->->

= goodbye 
	{ not amauld_present() || seen_recently(main): ->-> }
	~ with_amauld = false
	- (main)
		<- done_talking
		*	El:		Be seeing you, Amauld.
			Amauld: Always a pleasure.
			{ amauld_shop.hub.toy:
				Toy: 	Don't let the door hit you too hard as you go.
				Amauld:	Hush, Machellio. She's a good customer, in her way.
			}
	- ->->

= hub
// conversation hub. Local hub, outside main system
	<- done_talking 	
	*	El:		Have you heard any rumours about unknown sites?
		Amauld: I'm an engineer, not a guide. 


	-	-> hub

