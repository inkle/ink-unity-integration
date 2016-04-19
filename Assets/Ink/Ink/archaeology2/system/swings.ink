
CONST INITIAL_SWING = 1001

=== function raise(ref x)
	~ x = x + 1000
	

=== function push(ref x)
	~ raise(x)
	

=== function lower(ref x)
	~ x = x + 1 
	

=== function pull(ref x)
	~ lower(x)
	

=== function upness(x)
	~ return x / 1000

=== function downness(x)
	~ return x % 1000


=== function high(x)
	~ return (1 * upness(x) >= downness(x) * 9)

=== function up(x)
	~ return (4 * upness(x) >= downness(x) * 6)

=== function down(x)
	~ return (6 * upness(x) <= downness(x) * 4)
	
=== function low(x)
	~ return (9 * upness(x) <= downness(x) * 1)
	
