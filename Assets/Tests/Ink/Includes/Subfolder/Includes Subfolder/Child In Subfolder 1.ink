// This compiles here, but using the wrong files (somehow!), but not in Unity - although the Unity Integration code connects it properly
INCLUDE ../Child 2.ink
INCLUDE ../Includes Subfolder/Child In Subfolder 2.ink
// This compiles in Unity and in inky, but the Unity Integration code fails to connect it
// INCLUDE Child 2.ink
// INCLUDE Includes Subfolder/Child In Subfolder 2.ink

// This throws an error in Inky
// INCLUDE Child in Subfolder 3.ink

Child in subfolder 1