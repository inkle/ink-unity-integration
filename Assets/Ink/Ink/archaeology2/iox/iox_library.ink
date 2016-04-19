
=== function huang_present() ===
TODO: control Huang
	~ return true

=== function met_huang() ===
	~ return huang_in_library.huang_hub.done




=== iox_library ===

= enter
	-> set_events(-> library_tick) -> set_hub(-> hub) -> intro ->->

= intro
	The grand library of Iox is a wide dome filled with a gentle hush.
	{huang_present():
		At one desk, {met_huang():sits Huang|a man is a reading a book}.
	}
	->->

= library_tick
	{huang_present():
		-> huang_in_library.huang_tick ->
		{ not silent(): ->-> }
	}
	{
		- 	not shh:	 -> shh
		-	shh && not can_i_talk_now.youtalk && spoken_to_huang_recently(): -> can_i_talk_now.youtalk 
	}
	->->
	- (shh)
		Six: Mistress?
		*	El:	Shh.
			El:	It's a library. 
			Six:	Yes, Mistress. Apologies. 
		*	El:		Yes? 
			Six:	Do you intend to be inside here for very long?
			* * 	El:		No.
			* * 	El:		Why?
			* * 	El:		Perhaps, if I find anything interesting.
			- - 	Six:	It is very dark. I might require to leave to recharge. 
			* * 	El:		You don't need my permission.
			* * 	El:		Fascinating. 
			* * 	El:		All right. But stay close if you go. 
					Six:	Yes, Mistress. 
	- ->->

= exit_to_plaza 
	-> set_events(-> leaving_library) -> goto(IOX_PLAZA) -> iox_plaza

= hub 
	{ huang_present():
		<- huang_in_library.huang_hub
	}
	*	[Books - Look]
		El:		This place has all the books in the Delta. 
		El:		At least, as far as anyone knows. 

	+	{not seen_reasonably_recently(-> search_books)} [Books - Search for Information]	
		-> search_books

	+	[Doorway - Walk]
		{huang_present():
			Huang: 		Be seeing you.
		}
		-> exit_to_plaza

	-	->->

= leaving_library
	{
		- library_tick.shh && not can_i_talk_now: -> can_i_talk_now
		- else:	-> library_tick
	}
	->->

= can_i_talk_now
	Six:	Can I talk now, Mistress?
	* 	{not youtalk} El: 	Not yet. 
		- - (youtalk)
		{spoken_to_huang_recently():
			~ raise(tension)
			Six:	I observe you do not seem bound by the convention of silence yourself, I observe.
		}
	*	El:		Why not? 
		{huang_present() && met_huang():El: 	Huang won't mind, I'm sure.}
		Six: 	Thank you, Mistress.
	-	->->

/*------------------------------------------------------------
	Searching books for reference
------------------------------------------------------------*/

=== function six_is_reading() ===

	CONST HyafBook = 1
	CONST WellspringsBook = 2

	VAR books_six_has_read = 0
	VAR six_reading_book = 0

	~ return six_reading_book


=== search_books == 
= top 
	El::		Now, let's see...
	-> opts 
= opts 
	{not six_is_reading():
		+	 ->
			El::	 	Nothing else is jumping out at me. 
			->->
	}

	+	{six_is_reading()} 	[Six - Wait]
		>>> Six whizzed through the book while I waited.
		-> six_reads_books.six_finishes_book 
	+	{six_is_reading()}	[Library - Return]
		>>> I paced back away from the shelves while Six read.
		-> goto(IOX_LIBRARY) ->


	*	{not six_is_reading()} {heard_of(HYAF)} 	El:		Anything on Hyaf?
		-> read_book(HyafBook)

	*	{not six_is_reading()} {wellspring} 	El:		Wellspings...
		-> read_book(WellspringsBook)
		

	-	->->

= read_book(this_book)
	{ cycle:
		- El:	Ah, here.
		- El:	Here's something. 
	}
	{ huang_present():
	 	{
	 		- this_book == WellspringsBook && huang_in_library.huang_hub.wellsprings:
				>>> Huang looked up, caught sight of the book, and nodded to himself.
				>>> Then he looked down once more.
			- this_book == HyafBook && huang_in_library.huang_hub.hyaf:
				-> huang_in_library.snark_about_hyaf_book -> 
		}
	}

	-> six_reads_books.six_offers_to_read(this_book)
	{ six_is_reading(): ->-> }

	{this_book:
		- WellspringsBook:
			El::	It seems to be mostly stuff I know.
			El:: 	Wellsprings are the source of all the water in the Delta. 
			El:: 	It pours out, and is whipped out into space, where it freezes.
			El::	Looks like the author think the wells are fed from the starlight itself. 
		- HyafBook:
			El::	Seems like a nothing kind of a planet. 
			El::	Farms. Important, once, but long ago. 
			{huang_in_library.huang_hub.hyaf: 
				El::	Not obvious why Huang even went there.
			}
	}
	
	* 	[Bookshelf - Put book back]
		>>> I put the book away.

	* 	{not six_is_reading()} [Six - Hand over the book]
		El:		Take a look.
	 	~ six_reading_book = this_book
	 	Six:	Mistress. 
	- 	->->
	
=== six_reads_books ===

= six_offers_to_read(this_book)

	Six: 	Would you like me to read it for you, Mistress?
	* 	El:	Thanks. 
		~ six_reading_book = this_book
		
	* 	El:		I'll look at it myself. 
	-	Six: 	Mistress.
		->->

= huang_snark_six_reads
	Huang: 	You let him read for you, do you?
	*	El:		Six is faster than me. 
	*	El:		Sometimes. 
	-	Huang: 	What if he misses something?
	<-  huang_in_library.not_a_he
	*	El:		That doesn't happen.
		Huang:	You're sure of that, I see.

	*	El:		It's only a book.
		Huang:	Books are the only thing we have.
		Huang:	Everything else gets frittered away. 
	-	->->


= six_is_reading_tick
	{ not six_is_reading():
		-> DONE
	}
	{ seen_more_recently_than(-> six_finishes_book, -> six_pages):
		-> six_pages
	- else:
		-> six_finishes_book
	}
	- (six_pages)
		>>> Six whizzed through the pages of the book.
		{huang_present() && not huang_snark_six_reads:
			-> huang_snark_six_reads ->
		} 
	->->


= six_finishes_book 
	>>> Six put down the book. 
	~ temp book_read = six_reading_book
	~ setflag(books_six_has_read, six_reading_book)
	~ six_reading_book = 0

	Six: 	I have finished, Mistress. Would you like a summary?
	*	El:		That's all right.
		El:		Just update our ship when we get back.
		Six:	Mistress.
		->->
	*	El:		Go ahead.
		{ book_read:
			-	HyafBook:
					-> hyaf_book
			-	WellspringsBook:
					-> wellspring_book
		}
= hyaf_book 
	Six:	It is a brief and rather vague history of the Hyaf farming moon. 
	Six: 	It seems to have been flourishing at one time and was considered to be of strategic importance during the rebellions. 
	- (opts)
	*	El:		Worth visiting?
		Six:	Hard to say, Mistress. 
	*	El:		Does it provide a location?
		Six:	It does, Mistress. 
	*	{opts > 1} El: Anything else of interest?
		Six:	No, Mistress. 
		Six:	It seems the population there has been quite steady, and the settlement has been widely forgotten.
		->->
	*	El:		Thank you, Six. 
		->->
	-	-> opts
	

= wellspring_book
	Six:	A rather tedious outline of the dynamics of a wellspring, Mistress.
	Six:	The writer has studied a few wells to try and understand their rates of flow.
	- (opts)
	*	(socomplex) El:		What's so complicated about a well?
		Six:	When one finds something, it is natural to question where that thing comes from.
		Six:	He argues that the water from the wellsprings must come from somewhere.
		-> opts 
	*	{socomplex} El:		So where does it come from?
		Six:	He suggests it disperses into the starlight, flows over the edges of the Delta, and is from there somehow fed back to the springs.
		Six:	He presents little evidence for this conclusion.
		-> opts 
	*	{opts == 1} El:		Any interesting observations?
		-> onethingperhaps
	*	{opts > 1} El:		Anything else?
		- - (onethingperhaps) Six:	One thing, perhaps. 
			Six:	It seems the wells cease to flow at night, and resume once the sun has risen.
	-	->->
	


/*------------------------------------------------------------
	Huang in the the Library
------------------------------------------------------------*/

=== function huang_lost_locket() ===
	~ return false

=== function locket_on_table() ===
	~ return huang_in_library.produced_locket && not huang_in_library.puts_locket_away && not huang_lost_locket()

=== function spoken_to_huang_recently() ===
	~ return seen_reasonably_recently(-> huang_in_library.huang_hub.hi) || seen_reasonably_recently(-> huang_in_library.huang_hub.book)


=== huang_in_library 

= huang_tick 
	{
		- not produced_locket: -> produced_locket
	}
	->->

= not_a_he
	{done: 
		-> DONE
	}
	*	El:		Six isn't a he. 
		El: 	It's an it.
		Huang: 	Oh. I see.
		Huang:	You really do sail the Delta on your own, don't you?
	- (done)	->->

= produced_locket
	>>> Huang put down his book, and reached into a padded box to pull something out. A locket. 
	>>> He put in on the table beside him.
	->->

= snark_about_hyaf_book
	Huang: Is that book what I think it is?
	Huang: If I told you about a mythical moon whose rainfall is made from finest silk, would you go looking for it?
	- (opts)
	*	El:		(Ignore him)
		Huang: 	Well, perhaps one day I'll put it to the test. 
		->->
	*	El:		Is there such a planet? 
		>>> Huang laughed. 
		Huang: 	Hyaf is real, at least. Even if it is mostly mud.
		->->
	*	El:		For you, anything. 
		Huang: 	I'll bear that in mind. 
		Huang: 	It's more responsibility than I'm used to. 
		->->
	*	El:		Is it an interesting place, or isn't it?
		Huang:	It's largely unexplored, certainly.
		{not huang_lost_locket():
			Huang:	As far as I know, this little trinket is the only artefact from there.
			-> puts_locket_away ->	
		}
	- 	->->

= puts_locket_away
	>>> With that, he lifted the locket up, and folded it and the chain carefully into his pocket.
	->->

= huang_hub 

	*	(l) [Huang - Look at]
		El:: Huang Xe; I've met him a couple of times. He's one of the Professor's protegés. She likes her protegés old.
		>>> Huang looked up, and nodded, then returned to his book. 

	*	(hi) {l} El:		Huang. 
		Huang: 	Doctor.
		<- done_talking 
		* * 	El:	It's El. 
				Huang: 	I'll remember that, for when we next have a conversation.
		-  -	>>> He returned to his book. 

	*	(book)	{hi} El:		What are you reading?
		>>> He put down his book. 
		Huang:	You have nothing to do?
		* * 	El:		Looking around. 
				Huang:	And you thought you'd do it noisily in the library?
		* * 	El:		I'm talking to you, aren't I?
				Huang: 	So you are. 

	*	{locket_on_table()}	[Huang - Distract]
		El:		Huang, listen.
		Huang: 	Yes?
		* *		El:		I think I heard the Professor calling you.
		* *		El:		What time are you leaving here? 
				Huang:	(suspicious) Why?
				* * * 	{not up(honour)}	El:		Just wondering.
						Huang:	You have something of a reputation, you know.
						-> puts_locket_away ->
				* * * 	El:		I wondered if you wanted to go somewhere and talk.
						El:		You're not really meant to talk in a library.
				* * *	El:		I find your presence annoying. 
						Huang:	Do you?
						Huang:	Well, I'm sure your ship is just a hop away. 
				

	*	{locket_on_table()} [Locket - Look]
		El: 	I like your locket. Looks...
		* *		(val) El: ... valuable.
				Huang:	Not enough to steal, if that's what you were thinking. 
		* * 	El: ... unusual.
				Huang:	I've not seen many like it, no. 
		* * 	(great) El:	... great on you.
				- - (what_do_you_want) Huang: 	Now what do you want, I wonder?
				{ great: Huang:	I've never known you to pass a compliment for free.  }
		- - (opts)
		* * 	{opts == 1} {val}	El:	I'm glad to hear that.
				Huang: Just concerend for my safety, are you?
				-> what_do_you_want
		* * 	{not howyoupay} {val}	El:	I'm not sure what you're implying.
				Huang:	Sure you aren't. 
				- - (howyoupay) Huang:	We all know how you archaeologists pay for your ships, Doctor Ikrai.
				* * * 	{up(honour)} El:	Some. Not me. 
						Huang: 	Oh, yes. I forgot, you're a woman of honour.
				* * * 	{down(honour)} El:	We do the work to dig the things up, you know.
				* * *	El:		I hate to think how you pay for things, Huang.
		* * 	{great}	El:		I'm just saying it suits you.
				-> howyoupay
		* * 	El:	Where did you get it from?
			- - - (noharm) Huang:	I suppose there's no harm in telling you.
			- - - (hyaf) 		Huang:	Do you know Hyaf? Small moon, about halfway out. 
			* * * 	El:		I've never heard of it. 
			* * *	El:		There are a lot of moons out there. 
					Huang:	We're all moons in the Delta, as they say. 
			- - -	Huang:	It's a fairly unremarkable farming community. 
					Huang:	 I was there, oh, twelve years ago now.

	*	{hyaf} El:		Tell me about Hyaf. 
		>>> He put down his book.

		- - (wellsprings) Huang:	It's built on the ruins of a {wellspring()}. You know about wellsprings?
		* *		El:		What kind of archaeologist would I be if I didn't?
				Huang:	So you don't know about wellsprings, then?  Well, no matter.
		* *		El:		No?
		* *		El:		Is the well ruined now?
				Huang:	The temple above the well has collapsed, yes. But the water still flows.
				Huang:	Or at any rate, it did.
		* * 	El: 	Sounds like a rich place. 
				Huang:	In some ways, perhaps. But compared to this place...
				Huang:	They get by out there, I suppose. 
	-	(done) ->->
