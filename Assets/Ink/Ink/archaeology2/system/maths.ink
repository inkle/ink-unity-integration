
/* -------------------------------------------------
	Randomisation
------------------------------------------------- */

VAR __mathseed = 523330

=== function random(min, max) ===
    ~	__mathseed = ((__mathseed * 9301 + 49297) mod 233280 + 233280) mod 233280
    // note this is meant to be (max - min) * __mathseed / 233280
 
 	~ 	temp retVal = (__mathseed * (1 + max - min))  / 233280
 	
	//	Random seed {__mathseed} gave {retVal}, adding minimum {min} (max {max}).

 	~ 	return retVal + min

=== randomise ===
// seed the random number generator... if you want to!
= main 
	-> do -> do -> do -> do -> 
	//(Using random seed {__mathseed} for this game.)
	->->
= do
	{ shuffle:
		- ~ __mathseed = __mathseed + 2
		- ~ __mathseed = __mathseed +  67
		- ~ __mathseed = __mathseed +  2332
		- ~ __mathseed = __mathseed +  23
		- ~ __mathseed = __mathseed +  236
		- ~ __mathseed = __mathseed +  34
		- ~ __mathseed = __mathseed +  347
		- ~ __mathseed = __mathseed +  35
		- ~ __mathseed = __mathseed +  484573
		- ~ __mathseed = __mathseed +  346
		- ~ __mathseed = __mathseed +  2356
		- ~ __mathseed = __mathseed +  34757
		- ~ __mathseed = __mathseed +  437457
		- ~ __mathseed = __mathseed +  34735
		- ~ __mathseed = __mathseed +  235234
		- ~ __mathseed = __mathseed +  3324
		- ~ __mathseed = __mathseed +  357
		- ~ __mathseed = __mathseed +  3748
		- ~ __mathseed = __mathseed +  4574847
		- ~ __mathseed = __mathseed +  54746847
		- ~ __mathseed = __mathseed +  35233
		- ~ __mathseed = __mathseed +  3463467
		- ~ __mathseed = __mathseed +  3755685
		- ~ __mathseed = __mathseed +  23423
		- ~ __mathseed = __mathseed +  346457
		- ~ __mathseed = __mathseed +  23768
		- ~ __mathseed = __mathseed +  8636
		- ~ __mathseed = __mathseed +  45645
	}
	~ __mathseed = __mathseed mod 233280
	->->
	

/* -------------------------------------------------
	Maths
------------------------------------------------- */

=== function pow(x, n)===
	{ n == 0:
		~ return 1
	- else:	
		~ return x * pow(x, n - 1)
	}

=== function power2(n) ===
	~ return pow(2, n)

/* -------------------------------------------------
	Flags - indexed by index, not by 2^n
------------------------------------------------- */

=== function testflag(x, n) === 
// remove top half
	~ x = x mod power2(n + 1)
// will either be 1.blah or 0.blah  
	{ x / power2(n) >= 1:
		~ return true
	-	else:
		~ return false
	}

=== function unsetflag(ref x, n) === 
	{ testflag(x, n):
		~ x = x - power2(n)
	}

=== function setflag(ref x, n) === 
	{ not testflag(x, n):
		~ x = x + power2(n)
	}