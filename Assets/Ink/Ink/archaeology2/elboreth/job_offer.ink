
/* -------------------------------------------------

	Your annoying old lady neighbour takes a message for you: tunnel

------------------------------------------------- */


=== missed_job_offer ===

	>>> 	Just passing was my neighbour, an old lady. 

	Kati: 	You're back late tonight, Elesira.
	- (opts)
	*	(justel) El:		It's just El.
		Kati:	That isn't a name, it's a curse.
		- - (wherebeen) Kati: 	Where have you been, anyway? 
		-> opts

	*	{wherebeen} El:		Nowhere, really. 
		Kati:	Well while you were nowhere, I've been taking your messages.

	*	El:		I went to the bar. 
		Kati: 	If you want to find a man, that's not the place for a girl like you.
		* * 	El: 	I'm not a girl. 
				Kati:	If you're a woman that makes me a crone.
				-> goodtoknow
		* * 	El:		Please. You're not my mother.
				Kati: 	I might as well be. Taking messages for you.
	
	*	El:		How are you, Kati?
		Kati:	I've been better.
		Kati:	But I've been worse.
		- - (goodtoknow)
		* * 	El:		Good to know.
		* * 	El:		Same as usual, then?
				Kati:	Don't be so gleeful. You'll be old in the end.
		* * 	El:		Well, good night.
				Kati: 	Wait a minute. Not so fast. 
				Kati: 	You don't think I'm standing out here for my health, do you?
		- -		Kati:	I've got a message for you.
	- 	
	*	El:		And you've been waiting here to give it to me?
		Kati:	I have the young man my word, didn't I? Besides, what else would I be doing.
		* * 	El:		My thanks, then.
		* * 	El: 	What young man?
				-> someman
		- - 	El:	What's the message?
	*	El:		What message?
	*	El:		Who from?
		- - (someman) Kati:	Some man. He seemed quite well-dressed to be calling on you.
		* * 	El:		What did he want?
		* * 	El:		I doubt he was 'calling on' me.
		* * 	El: 	What was the message?

	-	Kati: 	He knocked on my door to ask if you were in. I said most likely not, if you weren't in.
		Kati: 	He asked me to give you this. Said he'd be back.
		>>> She held out a small business card. 
		~ atlas_card = true
		*		El:		What's this?
				El:		'Atlas Expeditions'. Do they want to give me a job?
				Kati:	He didn't say. 
		*		El:		I don't want it, whatever it is.
				Kati: 	I told him I'd give it to you, so here it is.
				Kati:	If you throw it away, that's your problem.
				
	-	Kati: 	Good night, Elesira.
		*	{justel} El:	Please call me El. 
			- - (someone_needs)		Kati: 	Someone round here needs to call you by your real name.
				Kati:	You shouldn't forget where you come from.
			* * 	El:		This isn't where I come from.
			* * 	El:		Good night, Katya.

		*	{not justel} 	El:	It's just El. 
			-> someone_needs

	-	>>> Kati hobbled back inside.
		->->

/* -------------------------------------------------

	Hassled by the company rep: tunnel

------------------------------------------------- */


=== function job_offer_story_beat() ===
	{ 
		- not job_offer.rep_talks: 
			~ return 1
		- not job_offer.epilogue && TURNS_SINCE(-> job_offer.rep_talks) > 0: 
			~ return 2
		- else: 
			~ return 0
	}


CONST NONE = 0
CONST DECLINED = 1
CONST CONSIDERED = 2
CONST ACCEPTED = 3


=== function job_offer_reply() ===
	{ 
		- not job_offer.rep_talks: ~ return NONE
		- job_offer.rep_talks.nothanks || job_offer.offer.no2: ~ return DECLINED
		- job_offer.offer.yesthanks: ~ return ACCEPTED
		- else: ~ return CONSIDERED
	}

=== function rep_calls_her_el() 
	~ return (job_offer.rep_talks.justel)

=== job_offer === 

	>>> On the corridor to my room, a man in a suit was waiting.

	->set_events(->do_job_storybeat) -> set_hub(-> hub)

	->->

= do_job_storybeat
	{ job_offer_story_beat():
		- 1:
			-> job_offer.rep_talks
		- 2:
			-> job_offer.epilogue
		- else: 
			->->
	}
	

= hub 

	+	{job_offer_story_beat()} [Els Door - Enter]
		-> do_job_storybeat
		
	+	{not job_offer_story_beat()} [Els Door - Enter]
		-> enter_room
	

	*	{not rep_talks}	[Man - Look]
		El::	He doesn't look right for this neighbourhood.
		
	*	{not rep_talks} 	El:	You want something?
		-> rep_talks

	- 	->->


= rep_talks
	-	Rep:	You're Elesira Ikrai?
		*	(justel) El:		Just El.
			Rep:	All right. El. I'm glad I caught you.
			-> imfromexp

		*	El:		Who are you?
			-> imfromexp 

		*	(iam) El:		I am.
			Rep:	Just back from the Sarkian Belt. You must be exhausted, so I won't keep you.

		*	(getaway) El:		Get out of my way, I'm tired.
			Rep:	Been busy out in the Sarkian Belt. I quite understand.
	- 
		* 	{getaway} 	El:		I doubt you do.
		*	{iam}		El:		No, you won't.
		* 	El: 	You don't look like an archaeologist. 

	-	(imfromexp) 
		Rep: 	I'm from {Cloudscape_Expeditions()}. I imagine you know our work.

		* 	El:		I've heard of your company, of course.
			El:		Although quite what you do is something of a mystery, isn't it?
		
		*	(notint) El: 	I'm not interested. 
			Rep:	You're certain of that?
			* * 	El:		Your company doesn't have the best reputation.
			* * 	El:		Yes. Whatever it is. 
			* * 	(pissoff) 	El:	Of course I'm certain.
					El:		I've had a long day and I'm very tired.
					El: 	Now go away before I'm rude.
					-> funded
		
		*	El: 	No.

	-	Rep:	We're in much the same line of work as you. We fund people, just like you, to sail to windward and see what hasn't been found.
		*	El:		And then you sell what you find.
			{ down(honour):
				Rep:	As do you, I suspect?
			- else:
				{ up(honour):
					Rep:	We do more than simply fence our finds, Doctor.
				- else:
					Rep: 	Do you not? I'd be surprised.
				}
			}

		*	El:	 	Are you here to threaten me?
			El: 	Back off our turf, is that it?
			Rep: 	No, nothing like that. 
			Rep:	The Delta belongs to everyone. We don't want to stand in the way of your explorations. Quite the opposite, in fact. 

		*	El:		You want to offer me a job?

	-	(funded) Rep:	You're funded by the university, is that right?
	- (chatopts)
		*	(nothanks) {getaway || notint || notgoing || pissoff} 
			El:	 	I'm serious. Get out of my way or I'll call security. 
			El:		It'll take him a few minutes to find his stick, but I can wait.
			Rep:	All right. I won't try to persuade you.
			{ atlas_card:
				Rep:	Did you get my card from the old lady? 
			else:
				Rep:	Here's my card. 
				~ atlas_card = true
			}
			Rep: If you change your mind, get in touch.

			>>> The man tapped his forehead in salute, and moved away along the corridor.
			§ Man goes.

			->->

		*	(notgoing) El:		I'm not going to work for you.	
			~ raise(honour)
			El:		It doesn't matter how much you pay. 
			Rep:	Just hear me out, that's all I ask.
			-> chatopts

		*	El:		Keep talking.
			~ lower(honour)

	-	-> offer ->
		->->

= offer 

	TODO: Rep makes a job offer

	- 	
		*	(no2) El:		I already have a job.
			Rep:	They're quite compatible.
			* * 	El:	 	I doubt that.
			* *		El:		Until I had to decide who to hand over my findings to.
			* * 	El:		I said no. 
			- - 	Rep: 	Very well, then. The offer will stay open.

		*	El:		Sounds good to me.
			Rep: 	So you'll take it?
			* * 	El:		Let me know where to be.
			* * 	El:		Why not?
			- - 	(yesthanks) Rep:	Excellent. My employers will be most pleased.
					Rep:	And you needn't tell the university about this.
			* * 	El:		Of course I'll tell them.
					Rep:	As you like.
					Rep:	But you don't need to.
			* * 	El:		Understood. 
			- - 	Rep:	Good night.

		*	El:		I'll think about it.
			Rep:	That's all I ask.

	-	>>> He turned to go, then paused.

	-	Rep: 	Oh. One last thing. Can I ask you... 
		Rep:	What are you looking for out there?
		*	El:		You can ask, but I won't answer.
			- - (swimmingly) Rep:	Well. I can see we're going to get along swimmingly. 
				-> goodnight

		*	El:		Something important.
			Rep:	And that's all you'll say?
			* * 	El:	Good night.
					-> swimmingly
			* * 	El:	Are we friends?
					Rep:	Not yet. Maybe one day we will be.
			* * 	El:	You wouldn't understand. 
					-> swimmingly
			- - 	-> goodnight

		*	El:		The answer. To the real question.
			Rep:	And what's the real question?
			El:		It's obvious, isn't it? 
			* * 	El:	...		Where did they all go?
					El:		This whole Nebula was populated, from end to end. Now it's a few planets, a few asteroids.
					El:		What changed?
			* * 	El:	... 	Where did they all come from?
					El:		We've found ships and buildings as old as anything out here.
					El:		Were we all born with ships?
			- - 	Rep:	Not a believer in the {Loop_Hypothesis()}, then.
			- - 	(opts)
			* * 	El:		I've never seen any proof for it.
					Rep:	You'd like to dig up your own bones, perhaps.
					* * * 	El:		That would make a convert our of anyone.
					* * * 	El:		Yours would do.
					* * * 	El:		Is that a threat?
							Rep:	Goodness, no. I don't make /veiled/ threats.
							-> goodnight
					- - - 	Rep:	Ha.
							-> goodnight
			* *		El:		There has to be a better explanation.
					-> maybe
			* * 	El:		What is the Loop Hypothesis, exactly?
					El:		People mention it, they way you did. Like it explains itself.
					Rep:	In a way, you have it. 
					Rep:	Loopers believe the artefacts we find are the ruins of our own civilisation. 
					Rep:	The past is our future and the future is our past.
					* * * 	El:		That's crazy.
					* * *	El:		Is that possible?
					* * *	El:		And where does it all come from?
							Rep:	Well, there's a question for the theologians.
							-> goodnight 
					- - - 	(maybe) Rep:	Maybe. But the Loop is neat. And comforting.
							Rep:	We'll all live again. Why not?
		-	(goodnight) Rep:	Good night, Ms Elasar.
			{ not atlas_card:
				Rep: 	Here's my card.
				~ atlas_card = true 
			}
			{
				- job_offer_reply() == DECLINED:
					Rep: In case you change your mind.
				- job_offer_reply() == ACCEPTED:
					Rep:	There's a dig tomorrow. Get in touch tomorrow and I'll send you the coordinates.
				- else:
					Rep: Get in touch. There's a dig tomorrow. You could be there.
			}

	- 	>>> 	The man moved away down the corridor. I couldn't hold back a shiver.
	
	-	->->

= epilogue
	>>> Then Six finally rolled up towards me.

	Six: 	Who was that, Mistress?
	*	{not down(tension)} 
		El:		And where were you?
		~ raise(tension)
		Six:	Apologies, Mistress, I was negotiating the stairs.

		* * 	El:		I thought you were supposed to protect me?
				El:		Someone jumps me from inside my room, and you're not there. 
				Six: 	My apologies, Mistress. I detected no danger.
		* * 	El: 	What were you doing?
				Six: 	I was following at a distance so as not to disturb you.

		- - 	Six: Mistress, have you been harmed?
		* * 	El: 	No, I'm all right.
				El: 	Perhaps a little shaken.
				Six: 	I am relieved to hear your are unhurt, Mistress. 
		* * 	El: 	Your timing is just bad, that's all.
		* * 	El: 	The sooner I get you chipped, the better.
				~ raise(tension)
				Six: 	Mistress, my Ethical Core cannot be chipped. 
			* * *	El:		Let's put that to the test, shall we?
					El: 	One of these days. 
			* * * 	El: 	You keep telling yourself that.

	*	{job_offer_reply() == DECLINED} 	 El:	No one.
	*	{job_offer_reply() != DECLINED}  El:		It doesn't matter.
	*	El: 	Atlas Expeditions.
		{offer:
			El:		They want to pay me.
		- else:
			El:		I think he wanted to offer me a job. 
			El: 	If he wanted to threaten me, he'd have done it.
		}
		Six: 	And will you consider it, Mistress?
		- - (willyouopts)
		* * 	El:		Are you asking as a friend, or a university asset?
				Six:	I cannot help being both, Mistress. 
				-> willyouopts
		* * 	El: 	I don't know.

		* * 	{job_offer_reply() == DECLINED}	El:	 Not a chance. 
		* * 	El: 	It sounds like it should be simple enough. 
				El:		If anything is ever simple.

	- 	El:		Come on.
		->->

= enter_room
	
	>>> Exhausted, I entered my room.
	-> goto(LOCATOR_INSIDE_ROOM) -> inside_room


/* -------------------------------------------------

	Called Atlas to get location of dig (leads to scene "flapping tents")

------------------------------------------------- */

=== called_atlas === 

:: TODO talk to atlasn and get coordinate to fly ot

->->
