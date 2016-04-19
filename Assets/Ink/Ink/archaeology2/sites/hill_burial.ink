VAR VILLAGE_EDGE = "VillageEdge"
VAR VILLAGE_CENTER = "VillageCenter"
VAR VILLAGE_FAR = "VillageFarSide"
VAR INSIDE_SLEEPERS_HUT = "InsideSleepersHut"
VAR OUTSIDE_CHAMBER = "OutsideChamber"
VAR INSIDE_CHAMBER = "InsideChamber"

=== burial_chamber_scene ===
	-> set_events (->burial_background_chat ) -> goto(VILLAGE_EDGE) -> village_edge -> begin_scene -> END

/*------------------------------------------------------------
	Basic Conversation
------------------------------------------------------------*/

=== burial_background_chat ===
	{with_six:
		{
			- not intro: -> intro
			- not what_make && inside_chamber.hub.left_again: -> what_make 
			- outside_chamber.can_you_get_through.squishsnakes && not ethical_snakes: -> ethical_snakes
			- village_conversation.poor_fields.state_life && not are_you_saying: -> are_you_saying
		
		}
		-> generic_Six_conversation ->
	}
	->->

= what_make
	Six:	So, Mistress? What do you make of the site?

	*	{sleepers_hut.hub.pipe} El:	I'm more curious about the sleepers in the hut.
		El:		Who's been supplying this place with Ashwater?
		Six:	It is quite curious, Mistress.

	*	{not inside_chamber.six_finished_reading} El:	What happened to the body?
		Six:	I do not believe there ever was a body, Mistress.
	*	{inside_chamber.six_scans_open_chest && not didnt_build_hopper} El:	That coffin wasn't made by these villagers. 
		-> didnt_build_hopper
	*	El:	Just a tomb. 
		{inside_chamber.six_finished_reading:
			Six: 	Of a God, no less, Mistress.
		- else:
			Six: 	Mistress.
		}
	*	El:	I have no idea, Six. 
		{ coffin_destroyed():
			El:	But at least we aren't empty-handed.
		}
	-	->->

= didnt_build_hopper
	El:		They didn't build a hopper.
	Six:	They re-used something that was already here. 
	El:	The question is...

	*	El:	... did the hopper still function?
	*	El:	... what was the coffin's original purpose?
		{inside_chamber.six_finished_reading.people:
			Six:	Connected to the tribute, perhaps?
		}
		Six:	Hoppers are purely short-distance. Effectively line of sight.
		- - (opts)
		<- done_talking
		* * 	{not orbit} El:		So there's something else here?
				Six:	Or there was. But I can detect nothing now.
				-> opts
		* * 	(orbit) El:		Something in orbit, perhaps.
				Six:	That seems very possible, Mistress.
				-> opts 
		* * 	{inside_chamber.six_finished_reading.people} El:	To take people - somewhere.
				Six:	It would be interesting to discover where, Mistress. 

		* * 	El:		I feel like we have unfinished business here, Six.
				Six:	Unlikely, Mistress. There is nothing else here.
	
	- 	->->


= are_you_saying
	*	El:		Are you saying you know how long I have to live?
		Six:	Know is a very strong verb, Mistress.
		- - (opts)
		<- done_talking 
		* * 	El: And what the hell does that mean? 
				Six: 	Mistress. 
				-> opts
		* * 	El:	You're not going to tell me?
				Six:	I am unable to, Mistress. 
				El:	But I will attempt to keep you safe all the same. 
		* * 	El:	All right. Forget it. 
				Six:	I fear I cannot, Mistress. 
				Six: 	But I shall ignore it.
	- ->-> 

= intro
	Six: 	The site is on the other side of the village, Mistress. 
	- (opts)
	*	(slipthrough) El:	We'll slip through.
		~ lower(honour)
		El:	No reason to involve the locals.
	*	El:	We'll see what they have to say about it. 
		Six:	They may not wish us to visit. 
		* * 	El:	Then we'll respect that. 
		* * 	El:	Then we'll convince them. 
		* * 	El:	We'll take that as it comes. 
	*	El:	Is the site much visited?
		Six:	Hard to be certain, Mistress. 
		Six:	My initial observation would suggest no.
		-> opts
	-	->->

= ethical_snakes
	<- done_talking
	* 	El:		Does your ethical core allow snake-squashing?
		Six:	Indeed it does.
		- - (opts)
		<- arrive
		* * 	El:		You're not supposed to not harm animals? 
				Six:	No.
				-> opts
		* * 	(suffer) El:		But they're alive. They suffer.
				Six:	I am not qualified to comment. 
				->opts 
		* *		{suffer} El:		You just don't care.
				Six:	My ethical core is not interested, certainly.
	- ->-> 
	
	
/*------------------------------------------------------------
	Village Conversation
------------------------------------------------------------*/


=== village_conversation ===
	{ how_many_people && not where_is_everybody && side_chat():
		*	El: 	Where is everybody? 
			-> where_is_everybody
	}
	{ not how_many_people && side_chat(): 		
		*	El:	How many people live here, would you say? 
			-> how_many_people
	}
	{ not poor_fields && side_chat(): 	
		*	El:		The fields here must be poor. 
			-> poor_fields
	}
	{ not agricultural && side_chat(): 
		*	El:		What kind of agriculture does this place have?		
			-> agricultural
	}
	{ poor_fields.eight && not poor_fields.done && side_chat(): 	
		*	El:	 	Eights months, you said?
			Six:	Give or take a week, depending on the relative health of the population.
			-> poor_fields.opts
	}
	{ poor_fields.eight && not poor_fields.baby_girl && side_chat(): 
		*	El:		What else can you tell about this place?
			Six:	A few minor points.
			-> poor_fields.baby_girl
	}
	-> burial_background_chat ->->

= where_is_everybody
	Six:	They are inside, Mistress.
	<- done_talking 
	*	{burial_background_chat.intro.slipthrough} El:	Well, let's not waste our chance.
	*	El:		But why?
		Six: 	I do not know, Mistress.
	*	El:		They're alive though, are they?
		El:		They're not all dead? 
		Six:	I do not think so, Mistress.
	-	->->

= agricultural
	Six:	Rice, potatoes. No wheat. Some livestock, but curiously little.
	*	El:		Not everywhere eats meat. 
		Six:	But almost everywhere requires wool for warmth.
	*	{not poor_fields} El:		The fields can't be very productive.
		-> poor_fields
	*	{poor_fields}	El:	They can't support them.
	- 	->->

= how_many_people
	Six:	Perhaps a hundred. Not much more. 
	-
	*	{not poor_fields} El:	The fields can't be very productive.
		-> poor_fields
	*	El:	The size of one apartment tunnel on Elboreth.
		Six:	And declining.
	*	El:		They must be all inbred. 
		Six:	It seems likely, Mistress. I see no evidence of hoppers or tethers.
		Six:	I do not imagine people here travel much.
	- 	->->

= poor_fields
	-	Six:	The ecology of this moon is worse than most, Mistress.
	- 	Six:	I can estimate how long it will be a viable for growth if you would like, Mistress?
	* 	El:		I can do without.
		Six:	As you wish, Mistress.
		->->
	* 	El:		All right. 
	* 	El:		I'd guess about five years. 
	-	(eight) Six:	Approximately eight months. The ground water reserves have perished.
	- (opts)
	<- arrive 
	<- done_talking
	*	El:		Do you just know these things? All the time?
		Six:	I calculated the figure at the moment when the concept of calculating it occurred to me.
		- - 	El:		So you know exactly how bad everything is, all the time. 
		<- done_talking
		<- arrive
		* * 	El:		Sounds a depressing way to live.

				Six:	Not all facts are depressing, Mistress. 
				- - - (baby_girl) Six:	For instance, one of the households here has recently had a baby. A girl.
				* * * 	El:		And that's not depressing?
				* * * 	El:		How can you possibly know that?
						Six:	There are certain hormones that are detectable in trace quantities for short periods.
				* * *	El:		What colour are her eyes?
						Six:	Most likely brown, Mistress. 
						Six:	Most people's eyes are brown.
		* * 	El:		Next you'll tell me how long I have to live. 
				Six:	No, Mistress.
				- - - (state_life) Six:	To state that would be against my Ethical Core.
	*	El:		We should tell them.
		Six: 	Mistress.
	-	(done) ->->

/*------------------------------------------------------------
	
	Village Map

------------------------------------------------------------*/


/*------------------------------------------------------------
	Outskirts
------------------------------------------------------------*/

=== village_edge ===
	-> set_events(-> village_conversation) -> set_hub (-> hub ) ->->

= hub 
	*	[Post - Examine]
		>>> 	The post marks the edge of the village. I ran my fingers over its inscriptions.
		- - (read_post) §PLACE OF GHOSTS§
		<- done_talking
		* *	El:		Strange name for a village. 
			Six:	They must know about the tomb.

	*	[Fields - Look]
		El:		Look at this place. 
		El:		The fields are almost barren. 
		{not village_conversation.poor_fields:-> village_conversation.poor_fields}

	*	[Hill - Look]
		El:		That's where the tomb is?
		Six:	I believe so, Mistress.

	*	[Houses - Look]
		{village_conversation.how_many_people:
			El:	Looks like this place was larger, once. The edge houses are empty.
		- else:
			El:		What's the population of this place?
			-> village_conversation.how_many_people
		}

	+	[Crossroads - Walk]
		El:	Come on.
		-> goto(VILLAGE_CENTER) -> village_center  

	-	->->

/*------------------------------------------------------------
	Center
------------------------------------------------------------*/


=== village_center ===
	-> set_events(-> village_conversation) -> set_hub (-> hub ) ->->

= hub 

	{ not outside_chamber || burial_background_chat.didnt_build_hopper.orbit:
		<- hut_hub
	}

	*	[Edge - Walk]
		{knock:
			{ not sleepers_hut:
				El:	Well, we tried. Let's go.
			- else:
				El:	Well, I suppose they won't mind. 
			}
		}
		-> goto(VILLAGE_FAR) -> village_far

	- 	->->

= hut_hub
	*	{not knock} [Hut - Look]
		El:		Poor-looking places. Few lights. 
		Six:	There are a few people inside that building.

	*	(knock) [Hut - Knock]
		El:		This seems like a likely one.
		Six:	Would you like me to do it, Mistress?
		* * 	El:	Thank you. I'm not afraid. 
		* * 	El:	I think you might scare the souls from them. 
		* * 	El:	Go ahead. 
				-> six_knocks
		- - 	>>> I knocked on the door. No response. 

	*	{seen_recently(-> knock)} El:	But you say it's not empty, Six?
		Six:	No. I sense seven or eight people inside. 
		* * 	El:		Seven or eight? What are you measuring?
				Six:	I'm listening to heartbeats. 
		* * 	El: 	Doing what?
				Six:	I cannot say, Mistress. But they appear quite restful.
		* * 	El:		Maybe they don't like visitors. 
				Six: 	That would seem likely, Mistress. 

	*	{knock} [Hut - Open]
		>>> I cast a glance at Six, then tried the door handle. 
		>>> It was locked. 
		Six:	I could break it down, Mistress. 
		* *		El:	Isn't that against your Ethical Core?
				Six:	No.
		* * 	El:	There's no need for that. 
			 	El:	I don't the owners would appreciate that. 
		* * 	El:	Go on then. 
				-> six_breaks_door

	+	{six_breaks_door} [Hut - Enter]
		-> set_events(-> enter_sleepers_hut) -> goto(INSIDE_SLEEPERS_HUT) -> sleepers_hut

	- 	->->

= six_breaks_door		
	>>> The robot didn't hestitate, but simply drove forward into the wood. It buckled and gave way. Inside, we saw several figures, prone on the floor. They appeared to be sleeping.
	->->

= six_knocks
	>>> Six raised a flipper and knocked while I took a step back. There was no reply. 
	Six:	Nothing, Mistress.
	->->

= enter_sleepers_hut 
	*	{burial_background_chat.didnt_build_hopper.orbit} El:	I wonder if there aren't any isn't a clue somewhere.
		El:	Something to connect this place with somewhere else. 
	*	{not sleepers_hut} El:	Hello? Is there anyone there?
		Six:	Evidently not. 
	*	{not sleepers_hut} El:		After you, Six.
		Six: 	Mistress. 
	*	-> arrive
	- 	-> arrive


/*------------------------------------------------------------
	Hut of Sleepers
------------------------------------------------------------*/

=== sleepers_hut ===
	§ Inside the hut. Seven people are sprawled out on the floor, asleep, around a bubbling hookah pipe.
	-> set_events(-> events) -> set_hub(-> hub) ->->

= events 
	{
		-	not hub.pipe && not ashwater: -> ashwater
	}
	- (ashwater)
		Six:	It seems we have interrupted some kind of gathering. 
		* 	El:	I don't think we've interrupted at all. 
		*	{village_edge.hub.read_post} El:	Place of Ghosts all right.
	->->

= hub 
	*	(pipe) [Pipe - Look]
		El:		Well, that explains it. Ashwater.
		El:		They must all be on it. 
		Six:	Why, Mistress?
		- - (pipe_opts)
		* * 	El:		Better than tilling a dying field. 
		* * 	(nochoices) El:		No choices left.
				Six:	This seems a curious choice to me, Mistress. 
				<- done_talking
				<- pipe_opts
		* *	{nochoices} El:	Do you dream, Six?
				Six: No, Mistress. 
				Six: Although I do hypothesise. 
				* * *	El:	That's not the same.
				* * * 	El:	I don't think these people are hypothesising.
		* * 	El:		I wonder where they got it from.
				Six:	It is not indigenous to this moon. 

	*	[Pipe - Scan]
		El:	What do you make of this, Six?

		TODO: Ashwater not local, can be traced, clue to next place.
		
		//{burial_background_chat.didnt_build_hopper.orbit:
		

	*	[Pipe - Turn Off]
		>>> I reached down and switched off the burner. The sleepers did not react. 
		El:	Don't want it burning the hut down. 

	*	El:	We've come to look at your tomb!
		>>> No response. 
		* * 	El:	All right. We've done our bit. 
				~ lower(honour)
		* * 	{not pipe && not lookover } El:	 There's something wrong here. 
		* * 	{pipe || lookover} El:	You don't mind? Fantastic.
				~ lower(honour)
		* * 	El:		Six, you can tell the Professor I asked for permission.
				Six:	Indeed, Mistress.
				Six:	I am not convinced you received it, of course.
				<- done_talking
				* * * 	El:	Fair point.
				* * * 	El:	That one nodded. 
						El:	I'm sure she did. 


	*	{pipe}	[Bodies - Look]
		El:	They'll be asleep for a week.

	*	(lookover) {not pipe} [Bodies - Look]
		El:		Are they...?
		Six:	Sleeping, Mistress. 
		* * 	El:		But you broke the door down. 
				-> indeed
		* * 	El:		They're sleeping pretty hard. 
				- - - (indeed) Six:	Indeed, Mistress. 
		* * 	El:		Not dead, then. 
				Six:	No, Mistress. 

	+	[Door - Leave]
		El:		Let's get out of here. 
		{ pipe:
			El:	Before the fumes start getting to me too.
		}
		Six:	Mistress. 
		-> set_events (-> village_conversation ) -> goto(VILLAGE_CENTER) -> village_center

	- 	->->

/*------------------------------------------------------------
	Far Side of Village
------------------------------------------------------------*/


=== village_far ===
	§ The far edge of the village, where a path rises up the hillside towards the tomb mouth.
	-> set_events(-> village_conversation) -> set_hub (-> hub ) ->->

= hub 
	*	{not outside_chamber} [Hill - Look]
		El:	Up there?
		Six:	Up there. You can just make out the stones of the entrance. 
		<- done_talking
		* * 	El:	All right, then.
		* * 	{sleepers_hut} El:	I'd feel better if we'd spoken to someone. 

	*	[Hill - Walk]
		-> set_events(-> walk_to_chamber) -> goto(OUTSIDE_CHAMBER) -> outside_chamber

	*	[Village - Return]
		-> goto(VILLAGE_CENTER) -> village_center

	-	->->

=== walk_to_chamber ===
	- (opts)
	<- done_talking 
	<- arrive
	*	El:		It's a some distance past the village. 
		Six:	Burial sites are usually placed at a respectful distance.
	*	El:		It's not still in use, is it? The tomb?
		Six:	There is no evidence for that. It appears quite small.
	*	{not sleepers_hut} {mostly_silent()} El:		Do you think anyone saw us?
		Six:	Not that I can tell, Mistress.
	- ->->

/*------------------------------------------------------------
	Outside the Chamber
------------------------------------------------------------*/

=== outside_chamber ===
	-> set_events (-> events) -> set_hub (-> hub) ->->

= events 
	{
		- not look_at_that: -> look_at_that
		- outside_chamber.hub.cracks && not can_you_get_through: -> can_you_get_through
		- not six_goes_inside: 
			Six:	If you would excuse me.
					-> six_goes_inside
	}
	-> burial_background_chat ->->

= look_at_that
	*	El:	 	Look at that. 
		El:		A passage tomb. Sealed to prevent robbers. 
	-	Six:	How do we get in?
 	* 	El:		We break the seal, of course. 
 		Six:	Are we robbers then, Mistress?
 		<- done_talking
 		* * 	El:		The tomb's owner has been dead a long time. It's only fair to bring thing back into circulation a little.
 		* * 	El:		After a fashion.	
 	*	El:		Can you scan anything through the rock?
 		Six:	It is rock, Mistress. No, I cannot scan through it.
 		{ up(tension):
 			Six:	My scanners are electromagnetic. Like your eyes. 
 		}
 	* 	El:		I'll see what I can do.
 	- ->->

= can_you_get_through
	*	El: 	Are you going to fit through the entrance?
		Six:	I will clear the way. 
		* * 	El:	 Don't break anything.
				Six: 	I will break as little as possible. 
				<- done_talking
				* * * 	(protect) El:		No. I said, don't break anything.
						~ raise(honour)
						Six:	Indeed, Mistress. 
						->->
				* * * 	El:		After you.
						-> thank_you
		* * 	El:	 You can go first. 
				- - - (thank_you) Six:	Thank you, Mistress. I would like to proceed you: there may be snakes. 
				-> opts

		* * 	El:		Why don't I go alone?
	*	El:		Why don't I go on in alone?

	- (snakes) Six:	I beseech your pardon, Mistress, but there may be snakes inside.
	- (opts)
	* 	El:		Are you good against snakes?
			Six:	Adequate, Mistress. I roll over them and they tend not to resist.
			- - (squishsnakes) // purely a marker
	* 	El:		I hate snakes. 
			Six:	Excellent, Mistress. 
			Six:	In that case, I will roll over them without a second thought.
			-> squishsnakes
	* 	(danger) El:	What makes you say that?
			Six:	No physical evidence, Mistress. More a slight sense of impending danger.
			-> opts
	* 	{danger} El:	But why snakes? Why not spiders? 
			Six:	Snakes live in dark holes from which they can strike, whereas spiders...
			* * 	El:		Never mind.
			* *		El: 	Go on.
						Six:	I am sure you know how spiders operate, Mistress. I will leave my explanation at that.
	- ->-> 
	-> burial_background_chat ->->
	
= hub 

	*	(leave1) {cracks} {with_six} {not smashed} El:	We should leave this intact.
		Six:	 Back to the ship, Mistress?
		* * 	El:	Back to the ship.
				-> leavenow
		* * 	El:	In a moment. 
	*	{leave1} El:	 All right, then. Time to go. 
		- - (leavenow) Six:	Mistress.
			-> end_scene


	*	(cracks) {not smashed} {not six_goes_inside} [Cracked Wall - Examine]
		>>> I ran my fingers over the cracks in the wall. There was a definite, slight breeze coming from inside.
		El: Whatever this is...

		* * El: ... it's open on the other side.
			Six:	 Most interesting.
		* *	El: ... can you get in?
			Six: 	It does not appear a very substantial barrier, Mistress.

	*	{cracks} [Cracked Wall - Brush]
		El:	Hand me the brush, Six.
		>>> I worked at the stone for a while, looking for any symbols or pictographs, but finding nothing.
		El:	Blank stone. 
		Six: 	A later addition, maybe?
		* *		El:	It's possible.
				El:	In which case, the tomb inside has most likely been thoroughly emptied.
		* * 	El: Or a hasty one. 	
		

	*	{cracks} {with_six} El:	You go first, Six. 
		>>> Mistress. 
		-> six_goes_inside

	*	[Distant Village - Look]
		El:		We're out of sight, aren't we?
		Six:	Not entirely, Mistress. 
		* * 	El:		We'd best be quick about it, then.
		* * 	El:		You let me know if anyone approaches.
		- - 	Six:	Mistress.

	*	(smashed) {not six_goes_inside} [Cracked Wall - Squeeze Through]
		>>> I turned myself this way and that, trying to find a way to squeeze through the narrow opening. Then Six moved over and simply struck the rock. It shattered.
		* *	{can_you_get_through.protect} El:	 What are you doing?
			Six: 	I require access as well as you, Mistress.
			-> can_you_get_through.snakes
		* *	El:	Big enough for you?
			Six: 	I believe so, yes. 
			Six:	Also for you. Mistress.
		* * El:	 I hope the other side of that wasn't carved or engraved.
			Six:	It would indeed be impressive for a corpse to engrave the interior sealing wall of its own tomb, Mistress.
			Six:	Such a thing would be a rare and unique find. To have destroy it would be terrible.
			* * *	El:	That's enough.
					Six: 	Yes, Mistress.
			* * * 	El:	Are you being sarcastic?
					Six: 	No, Mistress. 
					Six: 	But - such a wall would be rare enough, impossible even, that I do not fear it has happened today.

	*	{cracks} {not six_goes_inside} [Cracked Wall - Listen]
		>>> I put my ear against the crack. Nothing.
		{not with_six: ->-> }
		Six:	Mistress?
		* * 	El:		Nothing. 
		* *		El:		No ghosts. 
				El:		Or quiet ones, at least. 
		* * 	El:		Just checking. 


	+	{smashed || six_goes_inside} [Inside the Chamber - Enter]
		-> set_events(-> burial_background_chat) -> goto(INSIDE_CHAMBER) -> inside_chamber

	+	{inside_chamber} [Distant Village - Return]
		El:	Let's go back to the village. 
		-> set_events(-> burial_background_chat) -> goto(VILLAGE_FAR) -> village_far

	- 	->->

= six_goes_inside
	~ with_six = false
	{ not hub.smashed:
		>>> Pulling a set of a short metal bars from his inner compartment, Six set about levering chunks of the wall free. Dust trickled across the opening and I could not see what was inside. 
		Six:	That should be sufficient. 
	}
	>>> Six rolled away inside the chamber. 
	->->

/*------------------------------------------------------------
	Inside the Chamber
------------------------------------------------------------*/

=== function six_busy_reading_wall
~ return inside_chamber.six_reading_wall && not inside_chamber.six_finished_reading

=== function six_busy_scanning_box 
~ return inside_chamber.talk_about_empty_chest.analysing && not inside_chamber.six_scans_open_chest

=== function coffin_destroyed()
	~ return inside_chamber.six_strips_coffin

=== function six_busy_stripping_coffin()
	~ return inside_chamber.six_strips_coffin && not inside_chamber.six_strips_coffin_finished

=== function six_busy_inner_chamber()
	~ return six_busy_reading_wall() || six_busy_scanning_box() || six_busy_stripping_coffin()

=== inside_chamber ===
	~ with_six = true
	-> set_events (-> events) -> set_hub (-> hub) ->->

= events 
	{ six_busy_inner_chamber():
		{ six_busy_reading_wall():
			{ stopping:
				-	->-> // beat to waste time
				- 	-> six_finished_reading
			}
		}
		{ six_busy_scanning_box():
			{ stopping:
				- 	->-> // beat to waste time 
				- 	-> six_scans_open_chest
			}
		}
		->->
	}
	{
		- not how_long: -> how_long
	}
	
	- -> burial_background_chat ->->


= how_long
	El:	How long since anyone stood here, do you think?
	*	El:	... A hundred years?
		El:	A thousand? 
		Six:	Somewhat less, I should imagine.
	*	El:	... It's an incredible thought.
		Six:	Sadly highly credible, Mistress.
	*	EL:	... Can you scan for that?
		Six: 	I do not need to, Mistress.
	-	>>> He reached down and picked some up off the floor, turning it over in one of his pincers.
		Six:	A tobacco stub, I believe?
	*	El:		Kyla.
		Six:	I cannot be certain, Mistress.
		* * 	El:		I can.
				El:		It's got to be her.
		* * 	El:		Who else could it be?
		- -		Six:	The Delta is a large place, Mistress.
				Six:	But I take your point. In all likelihood, yes, it is Ms Kyla's.
	-	->->


= hub 

	{ not coffin_destroyed():
		<- coffin_hub
	}
	{ walls:
		<- metal_panel_hub
	}

	*	(xwall) [Walls - Examine]
		El:		Look at all this. It's beautiful. 
		El:		Stories and legends. The detail is fantastic. 

	*	(readwalls) [Walls - Read]
		>>> 	I skimmed over the images for a story I could grasp, something intact, and familiar, but there were too many fragments.

	+ 	(six_reading_wall) {seen_recently(-> readwalls)} El:	Six, can you make anything of these paintings here?
		{ six_busy_inner_chamber():
			Six: Please, wait, Mistress. I am occupied. 
			->->
		}
		Six:	I will take a look, Mistress.
		>>> Six began to scan meticulously across the images, his beams and tracers moving this way and that, like brushes across the ancient stone. 



	*	[Opening - Look Out]
		>>> At the doorway, I looked out across the hillside below. 
		El:		Good view of the village from here. It's more or less been build where it can be seen.
		{ six_busy_inner_chamber():
			->->
		}
		Six:	The extent of decline is also quite clear from here.
		* * 	{village_conversation.poor_fields.eight} El:	Eight months.
				Six:	Possibly less. No one is working. 
		* * 	El:	And the fields are empty. 
				El:	This place will be one big tomb soon enough.
		* * 	El:	What can we do for these people?
				Six: 	The ship will not carry a hundred people.

	+	{not six_busy_inner_chamber()} [Opening - Leave]
		{coffin_destroyed():
			El:	We'd better get out of here. 
			{ sleepers_hut:
				El:	Before anyone wakes up.
			}
		- else:
			El:		We've seen enough.
		}
		- - (left_again)
			-> set_events(-> burial_background_chat) -> goto(OUTSIDE_CHAMBER) -> outside_chamber


	*	(walls) {xwall} [Walls - Examine]
		>>> I looked over the walls of the tomb methodically, looking for traces of inscriptions or any differences in the blocks or materials. 
		>>> Sure enough, there was a small sheet of metal set around chest height in the far wall.
		El:	I think I found something.

	-	->->

= metal_panel_hub

	*	(examine_panel) {walls} [Metal Panel - Examine]
		>>> A metal panel. Touching it gently, it gave slightly at the base.
		El:	It's hinged. 

	*	{examine_panel} {not six_busy_inner_chamber()} El: 	Six, push this panel open. 
		Six:	Mistress.
		-> six_pushes_open_panel

	*	{examine_panel} [Metal Panel - Open]
		>>> I touched the panel and pushed it open, sliding my hand inside. 
		{ six_busy_inner_chamber():
			>>> Six looked up immediately from its work.
		}
		Six:	Mistress? I have the hopper ready.
		* *		El:	No poisoned spikes yet. 
		* * 	El: Thank you, Six. 
		- - 	El:	It's empty. There's nothing here. 
				{ six_finished_reading: 
					Six:	The God is dead. 
					{talk_about_empty_chest: 
						El:	And gone.
					}
				}
	-	->->

= coffin_hub	

	{open_chest && not six_busy_inner_chamber():
		<- talk_about_empty_chest
	}



	*	{not burial_background_chat.didnt_build_hopper} {six_scans_open_chest} El:	There's no way the villagers built this coffin, of course.
		-> burial_background_chat.didnt_build_hopper

	*	(xcoff) {not open_chest} [Coffin - Examine]
		El:		This is our host, then. 
		El:		There are some markings here, I think, but there's dust everywhere.

	*	{xcoff} {not open_chest} [Coffin - Blow] 
		>>> I leant over and blew the dust from the edge of the coffin. It rose in a swirling cloud and scattered over the floor.
		El:		The seals are still intact.
		{six_busy_inner_chamber():
			->->
		}
		Six:	Do you want me to open it?
		* * 	El:		No, thank you.
		* * 	El: 	Can you look inside without opening it?
				Six:	A little. May I?
				* * *	El:	Go ahead.
						-> scan_chest 
				* * * 	El:	Let it be. 
						El:	Let them rest. 
						Six:	You appreciate they are dead, of course.
						* * * *	El:	It's a human thing.
						* * * * El:	I would hope so.

		* * 	El:		Carefully.				
				-> open_chest	

	+	{scan_chest} {not open_chest} [Coffin - Open]
		{ stopping:
			- El:	Open it. 
			- El:	There's no point be squeamish about it. Open it up.
			- El:	Will you open it now?
		}
		{six_busy_inner_chamber():
			{stopping:
				- Six: 	Mistress, I will finish this inscription first. 
				-	>>> The robot simply ignored me.
			}
			->->
		}
		-> open_chest	
	-	->->	

= talk_about_empty_chest 
	*	(robbers) El:	Tomb robbers?
		Six: 	Impossible. Or at least, unlikely. 
		Six:	The coffin was sealed with lacquer, Mistress. It has not been opened.
		-> 	talk_about_empty_chest

	*	{not robbers} El:		Was it buried empty?
			-> empty
	*	{robbers} El:	Why would anyone seal up an empty chest?
		- - (empty)	Six: 	I cannot begin to imagine, Mistress. Perhaps they did not believe it to be empty.
		* * 	El:		Some kind of symbolic burial?
		* * 	El:		What's that supposed to mean?
		* * 	El:		No, there must be something else to it.
				Six:	I await your suggestions, Mistress. 
				->->
		- -	Six:	I do not pretend to understand humans in these matters, Mistress.
	*	(lookin) [Coffin - Look Inside]
		>>> I leant over and looked around inside, but couldn't see anything, not even dust, except a little in the cracks.

	*	{lookin} [Coffin - Scan Inside]
		El:	Scan inside, Six.
		Six:	There is nothing to scan, Mistress. This is an box empty. 
		* * 	El: 	And it's always been empty?
		* * 	{not up(tension)} 	El:		Whatever you can tell me. 
		* * 	{up(tension)} 		El:		Just do it, Six.
		* * 	El:		All right. 
				->->
		- -		Six: 	Very well, Mistress.
		>>> The robot leant over the empty casket and quietly probed it. 
		Six: 	This is most curious, Mistress.
		* *		El:	So there is something?
		* * 	El:	What is?
		- - 	(analysing) Six:	Please, wait, Mistress. I am analysing my results.
				->->


	*	{lookin} [Coffin - Brush]
		El:	Pass me the brush, Six. 
		>>> I worked on the edges and cracks of the coffin. A little settled dust, but nothing significant.
		Six:	No hidden holes, Mistress?
		El:		None that I can see.
	-	->->


= six_pushes_open_panel
	>>> The robot place one flipper into the opening in the wall.
	Six:	My arm is a remarkably good fit, Mistress. 
	- (opts)
	*	El:		Just how old are you, Six?
		Six:	I have only my memories to go on for that, Mistress.
		Six:	What more do you have?
		* * 	El:		I'm an orphan, you know that. 
				- - - (asamI) Six:	As am I, after a fashion.
	*	{not down(tension)} {asamI} El:	No, Six. You're a robot.
		El:	Now get on and tell me about that panel.
		~ raise(tension)
		Six:	Mistress.
		* * 	El:		I've found a few grey hairs.
	* 	El:		Do you feel anything?
	-	Six:	There are connectors inside the opening, but they are unpowered.
	- (opts2)
	*	El:		Can you tell what they were for?
		Six:	No.
	*	El:		Can you tell what they connect to?
		Six:	None at all. 
	*	El:		Can you repair them?
		Six:	I have no idea where the power would be sourced from, Mistress.
	*	{loop} El: 	Anything you can tell me?
	- (loop) {-> opts2 | }
	-	Six:	One thing, however. There is a motor on the panel.
		Six:	I believe it was designed to open itself.
	*	El:		Why?
		Six:	To receive something? Or to emit something. 
	*	El:	 	So there was so system active here. 
		El:		Before this place was made into a tomb, most likely. 
	-	Six:	Apologies, Mistress, but I have no idea. 
		->->
	


= six_finished_reading
	Six:	Mistress. I believe I have some sense of it.
	*	El:		Tell me a story, Six.
	*	(summary) El:		Give me the summary.
	- 	Six:	This is a God's tale. They came from a place of much water. 
		Six:	They made the fields fertile. 
		<- done_talking
		* 	El:		So far, so God.
			Six:	Indeed, Mistress. 
		*	{summary} El:		I said, summarise.
			-> killed
	-	(tithe) Six:	The village rose up to worship the God, who ruled from the hill.
		Six:	It looks like they paid some kind of tithe. 
		*	(food) El:		Food?
			Six:	No. Or, perhaps yes. 
		*	El: 	Go on. 
			Six:	The God demanded a tribute. I cannot tell how often. 
	- 	(people) Six:	People.
	-	(killed) Six:	Then the God was killed.
		*	El:		Killed? Are you sure about that?
			Six:	No. That is the inference, but it is only a pictogram.
		*	El:		That's a surprise. 
			El:		I didn't think Gods stood for being killed. 
		*	El:		The villagers fought back? Good for them.
	-	Six:	This is the God's tomb.
		*	{talk_about_empty_chest} 	El:	So where is she?
			Six:	It is only a story, Mistress.
		*	{village_conversation.poor_fields} El:	Doesn't look like it worked out for the villagers, did it?
		*	{not open_chest} El: So shall we open up and see a God?
		*	El:	Anything else?
			{  not tithe:
				Six:	It seems the God was killed for demanding a tribute. People.
			- else:
				Six:	Not that I can gather.
			}
	-	->->


= scan_chest
	
	>>> Six moved over to the chest and raised his arms, like some kind of praying mantis, about to strike. 
	*	El:		What can you see?
		Six:	Please, Mistress. I am listening.
	*	El:		(Let it work)
	-	>>> The robot moved this way and that around the chest, as though performing some strange ritual, commending the soul within to the stars. It was absurd. 
	{up(tension):
		>>> Finally, the performance stopped.
	- else:
		>>> Finally, it finished, and the robot turned to me once more.
	}
	-	Six: Mistress. There must be some error.
	*	El:		What do you mean?
		Six:	There must be some mistake.
	*	El:		Just tell me what you saw. 
		~ raise(tension)
		Six:	Nothing.
	*	{down(honour)} El:	Nothing valuable. 
		Six:	Nothing at all.
	-	Six:	This coffin - this box - is empty.
		Six:	There is no one inside.
	->->

= open_chest
	>>> Six rolled over, and removed pincers from his toolcase. Then he began to gently raise the lid. Dust billows out from inside. 
	{ scan_chest:
		Six:	As predicted.
	- else:
		Six: 	Mistress. The box is... 
	} 
	>>> Empty. 
	Six:	Empty. 
	->->

= six_scans_open_chest
	>>> The robot leant over the chest, and searched slowly up and down its length.
	Six:	The coffin is lined with quantium mesh, Mistress. 
	* 	{not quantium_used_for_hoppers} El:		Meaning what?
		Six:	It is the same mesh found in a hopper ring, Mistress.
		~ quantium_used_for_hoppers = true
	* 	{quantium_used_for_hoppers} El:		The same as in a hopper?
		Six:	Indeed, Mistress.
	- (opts)
	<- done_talking
	*	(hoppy) El:	Is that why it's empty?
		El:	Because it's a hopper?
		Six:	Perhaps so. 
	* 	{hoppy} El: 	Is it still active?
		Six:	There is no power. 
	*	(valuable) El: 	Quantium is valuable. Can we strip it out?
		~ lower(honour)
		Six:	I could cut it free, Mistress. Needless to say...
		* * 	El:	Yes?
				Six:	It would destroy the integrity of the coffin.
		* * 	El:	If it's needless, don't say it.
				Six:	Mistress.
	*	{valuable} 	El:	Strip it, Six. 
		Six:	Mistress. 
		-> six_strips_coffin
	- 	-> opts

= six_strips_coffin
	>>> The robot set to work with a mining cutter.
	->->

= six_strips_coffin_finished 
	Six:	Finished, Mistress. 
	TODO: acquire quantium resource
	->->
