
VAR OUTSIDE_CHAMBER_GEN = "OutsideBurialChamber"
VAR INSIDE_CHAMBER_GEN = "InsideBurialChamber"

/*------------------------------------------------------------
	Generic Burial Chamber
------------------------------------------------------------*/

=== generic_chamber_scene ===
	-> set_events (->gc_background_chat ) -> goto(OUTSIDE_CHAMBER_GEN) -> outside_generic_chamber -> begin_scene -> END

=== function gc_seen_after(-> x)
	~ return seen_after(x, -> generic_chamber_scene)

=== gc_background_chat ===
	{ not gc_seen_after(-> intro_chat):
		-> intro_chat
	}
	-> main_chat

= intro_chat
	// contextual choice of opening conversation
	Opening conversation.
	->->

= main_chat
	// asides 
	Main asides.
	-> generic_Six_conversation ->->
	

=== outside_generic_chamber ===
	-> set_events (-> events) -> set_hub (-> hub) ->->

= events 
	
	->->

= hub 

	+ 	(above_door) {not gc_seen_after(-> above_door)} [Doorway - Look Above]
		El: There's an inscription here.
	+ 	(brush_door) {not gc_seen_after(-> brush_door)} {gc_seen_after(-> above_door)}
		[Doorway - Brush]
		El:	Pass me the brush.

	+	[Chamber - Enter]
		-> set_events(-> gc_background_chat) -> goto(INSIDE_CHAMBER_GEN) -> inside_generic_chamber
	- 	->->




=== inside_generic_chamber ===
	-> set_events (-> events) -> set_hub (-> hub) ->->

= events 
	
	->->

= hub 

	+	[Doorway - Leave]
		-> set_events(-> gc_background_chat) -> goto(OUTSIDE_CHAMBER_GEN) -> outside_generic_chamber
	- 	->->	