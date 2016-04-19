
VAR BAR_DOOR = "BarDoor"

VAR BAR = "Bar"
VAR TABLE = "Table"

VAR timor = INITIAL_SWING
VAR drink_counter = 0
VAR drink_at = ""

=== function offered_to_fly_barkeep_to_neferi() === 
	~ return 	bartender_letter_from_rashid.offerflytoneferi

=== the_blue_monkey === 
= enter 
	-> set_events (-> NPC) -> set_hub(->hub) -> intro ->
	->->

= intro 
	
	§ El and Six roll into the Blue Monkey.
	{ stopping:
		- -> greeting1 
		- -> greetingN 
	}

= exit 
	{ up(timor):
		Timor: 	Hey, El!
		Timor:	Take care. 
	}
	-> goto(ELBORETH_MID_STREET) -> elboreth_street

= greeting1
	Timor: 	Welcome back. Still in one piece?
	* 	El: 	Just about. 
		Timor:	Sit down, have a drink. First one is on the house.
		<- 	done_talking
		* * 	El:		Sounds good.
		* * 	El:		I don't need your charity. 
				Timor:	Maybe not, but I need your custom. Sit down, El. 
	*	El:		It's quiet in here today. 
		Timor:	No change there, then.
	-	->->

	
= greetingN
	{ cycle:
		- 	Timor: 	Come on in, El. 
		-	Timor:	Evening, El. Sit anywhere you like.
		-	Timor:	Welcome back.
	}
	*	{semi_random()} El:		I've missed this place.	 
	*	{semi_random()} El:		Thanks, Timor.
	+	(notlong) El:		I won't stay long. 
		{notlong > 2:
			Timor: 	You always say that.
		- else:
			Timor:	As you will.
		}
	+	{mostly_silent()} 	El:		Great. 
	-	->->

= hub 
	+	{not serve_drink} 	[Bar Stool - Sit] 		
		-> serve_drink(BAR)
	+	{not serve_drink}  	[Table - Sit]			
		-> serve_drink(TABLE)

	+	{sale_artefacts_hub}	[Bar - Sell Artefacts] 	
		El:	I've got a few pieces for you to look at, Timor.
		-> sale_artefacts_hub

	*	[Pictures - Look]
		El:: Photographs of his children. They're all long gone now, of course.
		{ not bartender_letter_from_rashid: -> bartender_letter_from_rashid }

	-	->->



= sitting_hub	

	<- how_is_rashid
	<- where_is_everybody

	+	(drink) {drink_counter < 2} {drink_counter >= 0} 	[{drink_at} Drink - Drink] 	
		~ drink_counter++
		->->
	

	+	(finish_drink)  {drink_counter >= 2} 				[{drink_at} Drink - Finish] 						
		~ drink_counter = -1
		+ + 	El:		I'd better be going, Timor. 
				Timor: 	I'll see you around.
				-> exit
		* * 	[{drink_at} Drink - Push]
				Timor: 	Thanks.

	*	[Bar Door - Leave]
		- - (leave) El stands.
		El: 	I'll see you soon.
		Timor:	See you around, El.
		-> exit

= where_is_everybody
	*	El: 	So where is everybody?
		Timor:	People are feeling uncomfortable with the elections coming, I think. Aren't you?
		* * 	El:		If things go bad, I'll move to Iox.
				Timor:	They won't have you on Iox and you know it, El.
		* *		El:		If you talk this that, no wonder people don't drink here.
				Timor:	Ha. You might well be right about that.
	-	->->	




= serve_drink(x)
	- 	~ drink_at = x
		-> set_hub( -> sitting_hub ) ->

	Timor:	What will you have?
	+ 	El:		Tea.
		Timor: 	Coming right up. I've got a fresh plant in the deep-freeze.
	* 	(wine) El: 	Wine.
		- - (gotnice) Timor: 	I've got a nice bottle from Escara I've been saving...
	*	(sunflower) {not gotnice} El:		A Sunflower, please.
		Timor: 	I would love to, but when the last time we had gin?
		-> gotnice
	+	El:  	Just water. 
		Timor: 	Ah, you've been on a ship for a few weeks. I'll open a fresh bottle for you.
	+	El:		Nothing, but I've got a few things for you.
		-> sale_artefacts_hub
	-
	* 	El:	 	Don't waste it on me. 
		Timor: 	That kind of day? Whatever you say. 
		{wine: Timor: 	I'll get you something cheap with a burn to it. }
	* 	El: 	Sounds great. 
		El:		I could use something to pick me up a little.
	*	-> 
	-	->->

= find_anything
	-	Timor: So, did you find anything?
	*	El:		Out there? Not yet.
		Timor:	What are you hoping for?
		-> findwhat
	*	El:		I can't talk about it.
		~ lower(timor)
		El: 	It's university business. 
		Timor:	Doesn't knowledge belong to everyone?
		* * 	El:		Of course it doesn't.
				{ serve_drink.sunflower:
					El:		If I knew how to mix a decent Sunflower, do you think I'd come here?
					Timor: 	You're wounding me.
				else:
					El:		It belongs to whoever pays for it, for as long as they can keep it.
					Timor: 	Everyone knows everything in the end.
				}
				Timor: 	But you'd have to come up with some other excuse.
		* * 	El: 	Things aren't right at the university.
				Timor:	Things aren't right anywhere. 
				Timor:	But it's how you choose to act that matters. 
		* * 	El:		You're an idealist.
				{ not down(timor):
					El: That must be why I like you.
				- else:
					El:	I always wondered if I'd meet one.
				}
				-> totemple
	*	El:		A few things. 
		El:		But nothing major. Not what's waiting.
		Timor: 	And what's that?
		- - (findwhat)
		* *		El:		Do you have any idea how large the Cloud is, Timor?
				El:		Or how old? 
				El:		There are a thousand tiny worlds. There's so much to learn.
		* *		El:		The truth.
				Timor:	Ah, I see, as simple as that.
		* * 	El:		I don't know. There are so many questions to answer.
		- - 	(totemple) Timor: 	I went to the Loop Temple last week. It wasn't so bad. 
		* * 	El:		I despise that place. 
				~ lower(timor)
				El:		The {Loop_Hypothesis()} is the worst kind of half-think.

		* * 	El:		I thought better of you, Timor.
				~ lower(timor)
				Timor:	Not all of us are as strong-minded as you, Aqila. 
				Timor: 	You could see look into the stars and call space dark. You're a lion.
				* * * 	El:		An eagle.
						El:		It's what my name means. It's a bird from Maxiil.
						->->
				* * * 	El:		Should I roar?
						Timor: 	I'd run and hide if you did.
						->->
				* * * 	El: 	I'm a footsore archaeologist, nothing more.
						Timor:	The Temple isn't so bad, you know.

		* * 	El:		I'm glad it helped you.
				~ raise(timor)
		- -		Timor: 	You should come and hear them out sometime. We could go together.
		- - (outopts)
		* * 	El:		Are you asking me out?
				Timor: 	I'm trying to help you, Aqila. I see you're not happy.
				~ raise(timor)
				~ lower(honour)
				-> outopts
		* *		El:		What they teach at Temple goes against everything I believe.
				~ lower(timor)
				El:		I don't think it would help.	
		* *		El:		Thanks. I'll consider it.
				~ raise(timor)
				~ raise(honour)
				Timor: 	No pressure. I'll keep serving you whatever you decide.
			
	- ->->

= NPC 
	{
		-	not are_you_staying: 	-> are_you_staying
		- 	not bartender_letter_from_rashid: -> bartender_letter_from_rashid
		- 	not sale_artefacts_hub:	-> let_me_know		
		-	not find_anything: -> find_anything

	}
	- ->->
	- (let_me_know)
		Timor: 	I'm running low on things to sell, by the way.
		Timor:	In case you've got any pieces you don't need.
		*	El:		I'll show you what I've got. 
			-> sale_artefacts_hub 
		*	El:		Nothing right now.
		*	{up(honour)} El: Come on, Timor. I don't sell my finds. 
			~ raise(honour)
			Timor: 	Don't I know it. 
	-	->->

	- (are_you_staying)
		Timor: 	Are you staying?
		* 	El:		Not for long. 
			El:		There's more out there to explore.
		*	El: 	I'd like to.
			El:		But the university doesn't pay me to drink your Sunflowers. 
	- ->->

= sale_artefacts_hub
	TODO: sell stuff
	->->

/*------------------------------------------------------------------------
	Intro conversation about his son
------------------------------------------------------------------------*/

===  bartender_letter_from_rashid ===

= top
	{ 
		- the_blue_monkey.enter == 1: Timor:	I had a letter from Rashid last week.
		- else: Timor:	I had a letter from Rashid not long ago.
	}

	* 	El:		You did?
	* 	El: 	A real letter?
			Timor: 	A real letter, the silly boy. 
	- 	Timor: 	All his news, but by the time it reached me it was already out of date, of course.
	* 	El: 	You miss him?
		Timor: 	You don't have children, do you?
			* *		El:		No.
			* *		El: 	Apart from Six?
			- -	Timor:	In this whole system there's nothing like it. 
					Timor:	It's like someone glued your hands to a wild horse and then gave it a kick. 
	* 	El:		Where is he?
			- - (whereisrashid) Timor:	He's on Fenris. It's a slow ship to get there. I looked into it once.
			* *	(offerflytoneferi) El:		I could fly you there.
					El:		I have a ship.
					Timor:	I couldn't ask you to do that.
					* * *		El:		Why not? I go out almost every day.
					* * *		El:		You could, if you wanted to.
					* * *		El:		I think you're afraid to leave your bar.
								Timor: 	I think you're right!
								-> thinkaboutit
					- - - 	Timor:	Well, thank you.
					- - - 	(thinkaboutit)	Timor:	Look. I'll think about it.
			* * 	El:		You should do it one day.
			* * 	El:		It'll take a day at the most.
			- - 	Timor:	I'm not sure Rashid wants to see me. And I don't know how I'd find him.
	* 	El:		He'll come back.
		Timor:	Are you joking? Why would he do that?
		Timor:	If you got out of this hole would you come back?
		* * 	El:		I did. And I do.
				Timor:	Then you're mad, Elesira. There's nothing here worth coming back for. 
		* * 	El:		Where else is there to go?
				Timor:	Iox. I hear they have gardens the size of cities on Iox. 
				Timor: 	I hear they spend every weekend dancing.

	- 	->->

/*------------------------------------------------------------------------
	How is son?
------------------------------------------------------------------------*/


=== how_is_rashid === 

 	* 	{ seen_reasonably_recently(-> bartender_letter_from_rashid) } 		El:	So how is your son doing?
 		Timor:	Who knows? He says he's happy enough, but he doesn't want to talk to his father.
 		* * 	{not bartender_letter_from_rashid.whereisrashid} El:		Where is he?
 				-> bartender_letter_from_rashid.whereisrashid

 		* * 	El:		Why did he leave?
 				Timor:	Oh, to get away from his father's advice, I suppose. 
 				Timor:	More likely he was following a woman.
 				* * *	El: 	Did he mention a woman? 
 						Timor: 	No. 
 				* * *	El: 	Following his heart, perhaps. 
 						Timor: 	He always was a foolish boy.
 		- - 	Timor:	Oh, well. Wherever he is, I hope he is happy.

	*	{seen_reasonably_recently(-> bartender_letter_from_rashid) && not bartender_letter_from_rashid.whereisrashid }  
		{silent()} 					El:	Your son, Rashid. Where is he?
				-> bartender_letter_from_rashid.whereisrashid

	-	->->	
