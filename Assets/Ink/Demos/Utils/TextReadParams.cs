using System.Linq;
using UnityEngine;

// A basic, but effective, utility for estimating the reading time for a piece of text; Inkle has used this code on its last few projects. 
// It works by multiplying the number of characters by a constant (characterReadDuration). Terminator characters (.!?) are given a bit more time to read (terminatorDuration).
// It always adds extra time, allowing for the player to focus on the text and begin reading (extraTimeConstant).
// Finally, it clamps between a min and a max duration, which can be helpful for tuning.

// Something we can't stress enough - people have drastically different read speeds. Inkle games feature "read speed" sliders that slow animations and tune the numbers in this class.
// If you use this to auto-advance text, we also highly recommend allowing players to disable auto-advance, and note that players with fast read speeds greatly appreciate the ability to click to advance.
[System.Serializable]
public class TextReadParams {
    public float extraTimeConstant = 0.5f;
    public float characterReadDuration = 0.045f;
    public float terminatorDuration = 0.1f;
    public float minDuration = 1f;
    public float maxDuration = 4.0f;
    
    static char[] terminators = new char[]{ '?', '!', '.' };
    
    public static float GetEstimatedTimeToRead (string textStr, TextReadParams textReadParams) {
        var trimmedStr = textStr.Trim();
        int numRegularChars = 0;
        int numTerminators = 0;
        var lastWasTerminator = false;
        for(int i = 0; i < trimmedStr.Length; i++) {
            var isTerminator = terminators.Contains(trimmedStr[i]);
            if(isTerminator && !lastWasTerminator) numTerminators++;
            if(!isTerminator) numRegularChars++;
            lastWasTerminator = isTerminator;
        }
        if(lastWasTerminator) numTerminators--;
        
        var time = textReadParams.extraTimeConstant + numRegularChars * textReadParams.characterReadDuration + numTerminators * textReadParams.terminatorDuration;
        return Mathf.Clamp(time, textReadParams.minDuration, textReadParams.maxDuration);
    }
}