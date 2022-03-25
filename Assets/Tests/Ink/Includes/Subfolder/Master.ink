// This setup tests how Ink handles INCLUDE files.
// It tests...
// INCLUDE files that don't have the .ink extension
// INCLUDE files that INCLUDE other files
// INCLUDE files that use ../ to navigate up a subfolder
// A mix of the two!

INCLUDE ../Ancestor 1.ink
INCLUDE Child 1.ink
INCLUDE Child Without Extension
INCLUDE Includes Subfolder/Child In Subfolder 1.ink

Hello world!
	*	Hello back!
	Nice to hear from you!