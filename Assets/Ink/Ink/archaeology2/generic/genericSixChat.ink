=== generic_Six_conversation === 
// El questions
	{
		- high(tension) && down(honour) && not why_that_face && side_chat(): <- why_that_face
	}
	// a generic chat with Six block
	{ silent():
		{
			- not tell_me_of_ancestors: -> tell_me_of_ancestors
			- tell_me_of_ancestors && not where_did_ancestors_go: 
					Six:	The Ancestors, Mistress. Where did they go?
					-> where_did_ancestors_go

			- high(tension) && not fear_done_something: -> fear_done_something

			
			- up(tension) && not whatever_happened_to_five: 
					Six:	Mistress, if I may ask a question.
					-> whatever_happened_to_five

		}
	}
	-	->->

= tell_me_of_ancestors
	Six:	Mistress? Who were the Ancestors?
	*	El:		No one knows.
	* 	El:		Just people, most likely. 
	-	El:		They lived all across the Nebula, on all the smaller planetoids.
		<- arrive
	-	Six:	And where did they go?
		-> where_did_ancestors_go

= where_did_ancestors_go
//Six:	And where did they go?
	- (opts)
	*		El:		Several of these sites are unexplored. They may still be here.
			Six:	You cannot believe that, Mistress. There is no sign anywhere. No ships. No lights.
			->  opts
	*		El:		The sites are full of temples promising "ascension".
			El:		Perhaps that's where they went, in the end?
	*		El:		They moved off the asteroids, and onto the planets.
			El:		They're us. Well, not you, of course. 
	*		El:		That's the big question.
			El:		And wherever they went... are we heading there too?
	- ->->

= whatever_happened_to_five 
	Six:	Mistress? What did happen to Five?
	* 	El: 	Another time, maybe. 
		{raise(tension)}
	* 	El: 	Do you know the first Principle of Action?
		El:		If you throw something in space, then you are pushed back the other way?
		{raise(tension)}
		El:		That's what happened. 
		Six: 	Do I understand you used my predecessor as ballast, Mistress? 
		* *		El:		It was nothing personal.
		* *		El:		Five saved my life.
				Six:	Ah, so Five's Ethical Core prompted him to throw himself off the ship?
				* * * 	El:		Something like that.
				* * * 	El:		Right. Exactly.
		* *		El:		Wasn't the only thing I had aboard, either.
				~ raise(tension)
	- ->->

= why_that_face
	*	El:		Six, why do you have to have that face?
		Six:	I do not understand the question, Mistress.
	-
	*	El:		Never mind.
	* 	El:		You could have any face you like. 
		El:		It's not real. Why that one?
		- - (selected) Six:	My face is part of me, Mistress. It was uniquely created when I was built.
	*	El:		I don't like it. I don't like your face.
		~ lower(honour)
		~ raise(tension)
	-	Six:	I apologise if my face displeases you, Mistress. I fear I am unable to alter it.
	*	{selected} El:		How did they choose it?
		El:		Is it based on a person?
	*	El:		Shouldn't I be able to change it? To get what I want?
		Six:	I believe it is disallowed to prevent the substitution of one robot for another.
		Six:	Our faces are our fingerprints.
		-> finalopts
	-	Six:	No, Mistress. 
	-	Six:	I believe it is based on various minor variables that can be adjusted to provide a range of results.
	- (finalopts)
	*	{up(empathy)} El:		So why does your face look like my ex-husband?
		- - (accident) 	Six:	Was your husband in an accident, Mistress?
		* * 	El:		Yes, but not the way you mean.
		* * 	El:		I'm not going to talk about it to you.
		* *		El:		Was that a joke, Six?
		- - 	Six:	I fear I have mis-spoken, Mistress. I will withdraw from this conversation to allow you to restore your emotional equilibrium.
		* * 	El:		Good idea. 
		* * 	El:		I'm fine, I don't need to restore anything.
		- -		Six:	Indeed, Mistress.
	*	El:		Well, I don't like it. Can't you turn it off?
		~ raise(tension)
		Six:	It is believed that the presence of a face ensures a better connection between robot and owner.
	*	{not up(empathy)} 	El:	But that face. It can't be a coincidence.
		El:		The Professor must have picked it because it looked liked my ex-husband.
		-> accident
	- (done) ->->

= fear_done_something
	Six:	Mistress? Have I angered you in some way?
	Six:	I fear our relationship is not entirely cordial.
	*	El:		Your presence irritates me.
		~ raise(tension)
	*	El:		It's not one thing.
	*	El:		I'm sorry, Six.
		El:		The world is a tough place sometimes, 
		- - (apologyopts)
		* * 	El:	... but it's not your fault.
				Six: 	You mean, there is nothing I can do to alter the situation.
				Six: 	I understand perfectly.
				-> endof
		* * 	El:	... but I shouldn't take it out on you.
		* * 	El:	... and I didn't ask to have you with me.
				Six:	I did not ask for the assignment.
				Six:	However, Mistress, I am committed. 
				* * *	El:		So were the other five. 
						Six:	Mistress?
						<- done_talking
						* * * *		El:		No matter. Forget I mentioned it.
									Six:	Yes, Mistress. Of course, Mistress.
									-> endof
						* * * *		El:		You're not the first robot I've had tailing me.
									El:		I'll get rid of you in the end.
									-> endof
				* * *	El:		Don't I know it.
						-> spying
				* * *	El:		And why is that, I wonder?
						Six: 	Mistress? It is in my programming.
						* * * *		El:		We must do something about that.
									-> endof
						* * * *		El:		Of course.
									-> spying
						* * * *		El:	But Six, they keep on giving me robots.
									El:	Every time I break or lose one, they give me another.
									* * * * *	El:	... I can't figure out why.
									* * * * *	El:	... They must take my safety very seriously.
									* * * * *	El:	... It's like they're trying to get rid of them.
									- - - - -	Six:	They care for your well-being, Mistress.
				- - -	(spying) Six: 	And of course, I provide more accurate expedition reports than you might compile.
							<- done_talking
				* * * 	El:		Perhaps you're right.
							El:		No one else at the university is stupid enough to do what I do.
							{ up(honour): 
									El:		They need me in one piece for that stupidy, I suppose.
								- else:
									El:		They'd be stuck if they lost me.
							}
				* * *	El:		So you're a spy?
						Six:	I consider myself a research assistant, Mistress.
						* * * *		El:		Some research assistant.
										{up(honour):
											El:	You're never going to rise to greatness, are you?
										- else:
											El:	I can't have an embarrassing affair with you, can I?
										}
										Six:	No, Mistress. 

						* * * *		El:		Is that what you want?	
									El:		What you *really* want?
									Six:	Mistress?
									* * * * *		El:		Or are you just copying what I do?
									* * * * *		El:		Do you really want to research?
									* * * * *		El: 	Never mind.
													-> endof
									- - - - -		Six:	I gain pleasure from understanding, Mistress.
													Six: 	That makes me well-suited to research.

				- - - 	-> endof

		- - 	Six:	If such action assists you, Mistress, then I understand.
				Six:	But thank you for your explanation.
				-> endof

	-	El:		But you limit me at every turn.
		Six:	Mistress, I have saved your life repeatedly.
		*	El:		You worry too much.
		* 	El:		I could look after myself.
		*	El:		You're right, I suppose.
				~ lower(tension)
	-	Six:	And I provide valuable scanning and recording information.
		Six:	Not to mention, I also carry your brushes.
		*	El:		I'm not going to apologise to a robot.
			~ raise(tension)
			El:		So you'd better get used to it.
			Six: 	Yes, Mistress. Of course, Mistress.
			->->
		*	El:		So you're a scanner in a hip-bag.
			Six: 	And a guardian and a protector, Mistress.
			-> spying
		*	El:		You're very helpful, Six.
			El:		I lose my patience sometimes, 
			-> apologyopts
	- 	(endof)
		<- done_talking
		* 	El:		Now, don't mention this again.
			Six:	Certainly not, Mistress.
		*	El: 	Anyway. Let's see if you and I can do better.
			Six:	Thank you, Mistress. I would appreciate that.	
			~ lower(tension)
			~ lower(tension)
			~ lower(tension)
			~ raise(honour)			
	- ->->
