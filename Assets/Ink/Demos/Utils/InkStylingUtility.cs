using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

// Common (english) text utils
public static class InkStylingUtility {
    const string openItalics = "<i>";
	const string closeItalics = "</i>";
	static string OpenColor (string hexColor) => string.Format("<color={0}>", hexColor);
	const string closeColor = "</color>";
    
	static char[] nonTerminatorsWhichThenRequireTerminators = new char[]{ '"', '”'  };
	static char[] nonTerminators = new char[]{ ')', ']', '_', ' ', '\'', '’' };
	static char[] terminators = new char[]{ '?', '!', '.' };
	static char[] specialTerminators = new char[] { '?', '!' };
	static char[] punctuation = new char[]{ '?', '!', '.', ',', ';' , ':' };

    // static string builder for use anywhere in this class to save memory allocation.
	static StringBuilder sb = new StringBuilder();

	public static bool StartsWithVowel(string s) {
		if (s.Length > 0 && "aeiou".IndexOf(s.Substring(0, 1).ToLower()) > -1 ) {
			return true; 
		} 
		return false;
	}

	public static string UppercaseFirstCharacterHandlingQuotesAndOtherMarkers(string s) {
		if (string.IsNullOrEmpty(s)) return string.Empty;
		char[] newS = s.ToCharArray();
		var startOfSentence = true;
		char k;
		for (var i = 0 ; i < newS.Length; i++ )
        {
			k = newS[i];
			if (k == '\"' || k == '\'' || k == '_' || k == '‘' || k == '’' || k == '“' || k == '”' || k == '§' || k == ' ' ) continue;

			if (startOfSentence)
				newS[i] = char.ToUpperInvariant(k);
			
			startOfSentence = isTerminator(k);

			if (startOfSentence && i < newS.Length - 1 // not the end of the sentence
				&& (newS[i] == '!' || newS[i] == '?') // was ! or ? only
				&& (newS[i + 1] == '\"' || newS[i + 1] == '\'' || newS[i + 1] == '’' || newS[i + 1] == '”') ) // followed a close-quote of some kind
				startOfSentence = false;

			// ellipsis
			if (i > 2 && k == '.' && newS[i - 1] == '.' && newS[i - 2] == '.') startOfSentence = false;

		}

		return new string(newS);
	}




	public static string ParseStyling(string dialogueText, bool colourise = true, string hexColor = "#FFE473") {
		sb.Clear(); 
		
		bool inItalics = false;
		bool inColour = false;

		for (int idx = 0; idx < dialogueText.Length; idx++)
		{
			if (dialogueText[idx] == '_')
            {
				inItalics = !inItalics;
				sb.Append(inItalics ? openItalics : closeItalics);
				continue;

			} else if ( dialogueText[idx] == '§')
            {
				if (colourise)
				{
					inColour = !inColour;
					sb.Append(inColour ? OpenColor(hexColor) : closeColor);
				}
				continue;
			}

			if (dialogueText[idx] == ' ')
            {
				if (inColour) sb.Append(closeColor);
				if (inItalics) sb.Append(closeItalics);
			}

			sb.Append(dialogueText[idx]);

			if (dialogueText[idx] == ' ')
			{
				if (inColour) sb.Append(OpenColor(hexColor));
				if (inItalics) sb.Append(openItalics);
			}

		}

		return sb.ToString(); 
	}

    // Formats text to make it seem like the speaker is tired
	public static string ExhaustifyDialogueText(string dialogueLine, bool veryTired = false) {
		List<string> listOfTiredRepeatingWords = new List<string>() { "i", "you", "i'm", "but", "if" };
		List<char> listOfPunctuation = new List<char>() { '.', ',', ';', ':', ')', '-', '\'', '\"', '>' };

		// the good bit. turn dialogue into exhausted dialogue automagically
		// 1) repeat significant things 
		bool doneRepeatedPersonalIdentifier = false;
		int beatCount = UnityEngine.Random.Range(2, 5);
		var sb = new StringBuilder();
		var words = dialogueLine.Split(' ');
		foreach (string word in words)
		{
			if (!doneRepeatedPersonalIdentifier && listOfTiredRepeatingWords.Contains(word.ToLower()))
			{
				sb.Append(word + "... ");
				doneRepeatedPersonalIdentifier = true;
			}
			beatCount--;
			sb.Append(word);
			if (word.Length > 0)
			{
				var lastChar = word[word.Length - 1];

				if (beatCount <= 0 && !listOfPunctuation.Contains(lastChar))
				{
					sb.Append("...");
					beatCount = UnityEngine.Random.Range(2, 5) + (veryTired ? 0 : 2);
				}
				sb.Append(" ");
			}

		}
		return sb.ToString();
	}




    

	public static string SentenceCaseWithoutArticles(string s) {
		if (s.IndexOf("a ", StringComparison.OrdinalIgnoreCase) == 0) { s = s.Remove(0, 2); }
		if (s.IndexOf("the ", StringComparison.OrdinalIgnoreCase) == 0) { s = s.Remove(0, 4); }
		return UppercaseFirstCharacter (s);
	}

	public static string UppercaseFirstCharacter(string s){
		if (string.IsNullOrEmpty(s)) return string.Empty;
		char[] a = s.ToCharArray();
		a[0] = char.ToUpperInvariant(a[0]);
		return new string(a);
	}


	/// <summary>
	/// Procedural text functions include a leading 'n ' when attached to a word that needs "an" not "a" 
	/// This is now removed: we turn "\ba\s+n\b" into "\ban\b"; and then remove "\bn\b" 
	/// </summary>
	/// <returns>The text without article altering markers.</returns>
	/// <param name="dialogueLine">Dialogue line.</param>
	public static string ResolveArticleAlteringMarkers (string s) {
		// The following does
		//	 	/\b[aA]\s[nN]\s/  	=> 	/an /  (done below by skipping one write step)
		// then
		//		/\b[nN]\s/  		=> 	//		(done below by writing in pre-N character, then skipping the n and the space after)

		var inWord = false;
		StringBuilder outputString = new StringBuilder(s.Length); 
		for (var i = 0; i < s.Length - 1 ; i++ ){
			if (!inWord && (s[i] == 'n' || s[i] == 'N') && s[i+1]==' ') {
				// we've found an 'n'. 
				// Note we were just about to write in the "space/other" that came just before this N
				// But now we won't; we'll skip over it; and maybe skip the "n" too
				if (! (i >= 2 && s[i-1] == ' ' && ( s[i-2] == 'a' || s[i-2] == 'A' ))) {
					if (i >= 1) {
						outputString.Append(s[i - 1]); 
					}
					i += 2; 
				}
				continue; 	// Don't write in the space before either. 
			}

			inWord = Char.IsLetter(s[i]); 
			if (i > 0) {
				outputString.Append(s[i - 1]); // write in the character before this one
			}
		}
		if (s.Length >= 2) {
			outputString.Append(s[s.Length - 2]); 
		}
		if (s.Length >= 1) {
			outputString.Append(s[s.Length - 1]);
		}

		return outputString.ToString();
	}

	/// <summary>
	/// Trim, concatenate multiple white space, and ensure spacing before and after punctuation is correct
	/// </summary>
	/// <returns>The string without incorrect whitespaces.</returns>
	/// <param name="s">S.</param>
	public static string CorrectSpacingErrors(string s) {
		if (string.IsNullOrEmpty(s)) return string.Empty;
		var resultString = new StringBuilder(s.Length * 2);

		bool inStringOfPunctuationCharacters = false;
		bool inQuotes = false;
		bool inSingleQuotes = false;

		bool mightHaveClosedSingleQuote = false;

		char lastCharacter = '^';

		int fullstopCounter = 0;

		bool pendingUnderscore = false; // we aim to hold back underscores to after ,.;?! punctuation, so _yes_? becomes _yes?_ 

		for (int i = 0; i < s.Length; i++) {
			char thisChar = s [i];
			if (thisChar == '\'') {
				if ((lastCharacter == '^' || lastCharacter == ' ' || (inStringOfPunctuationCharacters && !inSingleQuotes)) && 
					(!inSingleQuotes ||  mightHaveClosedSingleQuote)) {
					thisChar = '‘';
					inSingleQuotes = true;
					mightHaveClosedSingleQuote = false;
				} else {
					if (inSingleQuotes) {
						// okay, take a decision. Is this closing the quote, or not? s
						if (isPunctuation(lastCharacter) || ( lastCharacter == ' ' && isPunctuation( resultString [resultString.Length - 2] ) ))
							mightHaveClosedSingleQuote = true;
					}
					
					if (inSingleQuotes && inStringOfPunctuationCharacters) {
						inSingleQuotes = false;
					}

					// we think this is an apostrophe
					thisChar = '’';

				}
			}
			// double quotes are easier because they have to alternate
			if (thisChar == '"') {
				if (inQuotes) {
					inQuotes = false; 
					thisChar = '”';

				} else {
					thisChar = '“';
					inQuotes = true;
				}
			}

			// no spaces after open-quotes
			if ((lastCharacter == '“' || lastCharacter == '‘') && thisChar == ' ') {
				continue;
			}

			if (pendingUnderscore && !isPunctuation(thisChar) && thisChar != ' ') {
				pendingUnderscore = false; 
				resultString.Append ('_');
			}
			if (thisChar == '_') {
				pendingUnderscore = true;  
				continue;
			}


			// note that open quote is counted as a word character, not a punctuation character
			if (isPunctuation(thisChar) || isQuoteMark(thisChar)) {
				// close single-quotes don't request a space after, but they don't insert one before either
				if (thisChar != '’') {
					inStringOfPunctuationCharacters = true;

					if (lastCharacter == '’') {
						// if we close a quote only to hit punctuation, it wasn't an apostrophe after all
						inSingleQuotes = false;
					}

				} 

				if (lastCharacter == ' ' && resultString.Length > 0) {
					// only applies to punctuation, or quotes that aren't leading 'postrophes
					if (!isQuoteMark (thisChar) || mightHaveClosedSingleQuote) {
					
						// the last character we wrote was a ' ', so delete it. 
						resultString.Remove ((resultString.Length - 1), 1);
						lastCharacter = resultString [resultString.Length - 1];

						if (!pendingUnderscore && lastCharacter == '_' && resultString.Length > 0) {
							pendingUnderscore = true; 
							resultString.Remove ((resultString.Length - 1), 1);
							lastCharacter = resultString [resultString.Length - 1];
						}

						// we've decided we've closed a quote after all
						if (mightHaveClosedSingleQuote) {
							inSingleQuotes = false; 
							if (isPunctuation (lastCharacter)) {
								inStringOfPunctuationCharacters = true; 
							}
						}
					}
				}

			} else { 
				// add space after runs of punctuation, so we don't space out ... marks or ." 
				// note we don't consider ' to be punctuation here.
				if (inStringOfPunctuationCharacters && thisChar != ' ') {
					if (thisChar != '_') {
						// allow _ after punctuation, so we can close italics tightly; but we're still looking for a space next turn
						resultString.Append (' ');
					}
				} 

				inStringOfPunctuationCharacters = false; 

			}

			// remove basic duplication 
			if (thisChar == lastCharacter && (thisChar == ' ' || thisChar == ',' || thisChar == ':' || thisChar == ';' || thisChar == ')' || thisChar == '('))  {
				continue;
			}
			// remove leading junk
			if (resultString.Length == 0 && (thisChar == ' ' || thisChar == ',' || thisChar == ':' || thisChar == ';' || thisChar == ')'))  
				continue;
		
			bool foundEllipsis = false;

			// slightly trickier, handle multiple full-stops but allow "..."  and "..?" and "..!"
			if (isTerminator(thisChar)) {

				if (lastCharacter == '.' ) {
					fullstopCounter++;
					if (i == s.Length - 1 || (s [i + 1] != '.' && s [i + 1] != '!' && s [i + 1] != '?')) {
						// we're at the end of this run of stops.
						if (fullstopCounter >= 3) {
							// is this the third stop in a row? If so, we printed the first; now append one more for the middle
							resultString.Append ('.');
							foundEllipsis = true;
						} else {
							// either there was a double stop, so treat as normal duplication, or we're still looking ahead 

							continue; 
						}
					} else {
						// basically do no duplication
						continue; 
					} 
				} else {
					fullstopCounter = 1;
				}
			}

			// don't allow weird punctuation strings
			if (!foundEllipsis && lastCharacter != thisChar && isPunctuation(lastCharacter) && isPunctuation(thisChar)) {
				// convert ,. into .  and ., into ,
				// Note this prevents ?! or !?, which is a bug. Are there any other similar use cases?
				if (!(isSpecialTerminator(lastCharacter) && isSpecialTerminator(thisChar)))
				{
					if (isTerminator(lastCharacter) != isTerminator(thisChar) && fullstopCounter <= 1)
					{
						fullstopCounter = 0;
						resultString.Remove((resultString.Length - 1), 1);
						if (resultString.Length >= 2)
						{
							lastCharacter = resultString[resultString.Length - 1];
						}
						else
						{
							lastCharacter = '^';
						}
					}
					else
					{
						continue;
					}
				}
			}

			if (pendingUnderscore && thisChar == ' ') {
				pendingUnderscore = false; 
				resultString.Append ('_');
			}

			resultString.Append(thisChar);

				 
			if (inSingleQuotes && lastCharacter == '’') {
				if (thisChar != ' ' && !inStringOfPunctuationCharacters) {
					// we printed a close quote before, and now a "real" letter
					// so it was an apostrophe 
					mightHaveClosedSingleQuote = false;
				} else if (thisChar == ' ') {
					// we might have just closed the open quote string after all
					mightHaveClosedSingleQuote = true; 
				}
			}

			lastCharacter = thisChar;

		}

		if (pendingUnderscore) {
			resultString.Append ('_');
		}

		return resultString.ToString().Trim();
	}

	public static bool isPunctuation(this char thisChar) {
		return punctuation.Contains(thisChar);
	}

	public static bool isTerminator(this char thisChar) {
		return terminators.Contains(thisChar);
	}

	public static bool isSpecialTerminator(this char thisChar)
    {
		return specialTerminators.Contains(thisChar);
	}

	public static bool isQuoteMark(this char thisChar) {
		return ( thisChar == '”' || thisChar == '’' || thisChar == '\'' || thisChar == '"' );
	}

	public static string Possessive(this string txt)
    {
		if (!string.IsNullOrEmpty(txt))
		{
			if (txt[txt.Length - 1] == 's')
				return txt + "'";
			else
				return txt + "'s";
		}
		return "";
	}

	/// <summary>
	///  Runs the punctuation-correction, spacing-correction and grammar adjustment tidy-up routines 
	///  required to make ink text presentable onscreen.
	/// </summary>
	/// <returns>The processed text.</returns>
	/// <param name="text">Text.</param>
	public static string ProcessText(string text, bool terminatingPunctuation = true, bool stripSquareBrackets = false ) {
		if (string.IsNullOrEmpty(text)) return string.Empty;
		var outText = text;
		if (stripSquareBrackets)
			outText = StripSquareBrackets(outText).Trim();
		outText = CorrectSpacingErrors(outText);
        outText = ResolveArticleAlteringMarkers(outText);
        outText = UppercaseFirstCharacterHandlingQuotesAndOtherMarkers(outText);
		outText = EnforceTerminatingPunctuation (outText, terminatingPunctuation);

		return outText;
	}

    /// <summary>
    /// Removes square bracketed sections of the string. Square brackets are used as comments and shouldn't make it to the screen.
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
	public static string StripSquareBrackets(string s)
	{
		var resultString = new StringBuilder(s.Length);
		var inBrackets = false;
		for (int i = 0; i < s.Length; i++)
		{
			char thisChar = s[i];
            if (thisChar == '[')
            {
				inBrackets = true;
			} else if (thisChar == ']')
            {
				inBrackets = false;
				continue;
            }

            if (!inBrackets)
			    resultString.Append(thisChar);
		}
		return resultString.ToString();
	}

	public static string AppendEllipsis(string text) {
		text = text.Trim (); 
		var lastChar = text [text.Length - 1];
		if (lastChar == '_') {
			return AppendEllipsis (text.Substring (0, text.Length - 1)) + "_";
			 
		} else {
			if (text.Length == 0) {
				return text; 
			} else if (!isTerminator (lastChar)) {
				if (!isPunctuation (lastChar)) {
					// no terminator? Just shove "..." on the end then
					return text + "...";
				} else {
					// end in a comma or something...
					return text.Substring (0, text.Length - 1) + "...";
				}	
 
			} else if (isTerminator (lastChar) && isTerminator (text [text.Length - 2])) {
				// two consecutive terminators is a valid ellipsis in crazy text world!? (also You... of course)
				return text;

			} else {
				var sb = new StringBuilder ();
				// eg. "You!"
				var terminator = lastChar; // '!'
				// You
				sb.Append (text.Substring (0, text.Length - 1));
				sb.Append ("..");
				// You ..!
				sb.Append (terminator); 
				return sb.ToString ();

			}	
		}
	}

	public static string EnforceTerminatingPunctuation(string s, bool requireTerminatingPunctuation) {
		
		if (string.IsNullOrEmpty(s)) return string.Empty;
		var resultString = new StringBuilder (s.Length + 1);

		// find the real terminating character, ignoring quotes and brackets
		var terminatorIndex = s.Length - 1;
		for (; terminatorIndex >= 0 ; terminatorIndex--) {
			if (nonTerminatorsWhichThenRequireTerminators.Contains(s[terminatorIndex])) {
				//  the presence of a close-quote means we now require punctuation inside that quote
				requireTerminatingPunctuation = true;
			} else if (!nonTerminators.Contains(s[terminatorIndex]))
				//  this character wasn't a post-terminator character of any kind; it's a terminator or content
				break; 
		}

		// no real text 
		if (terminatorIndex < 0)
			return s; 


		if (requireTerminatingPunctuation) {
			// want a terminator and already have one
			if (terminators.Contains (s [terminatorIndex]))
				return s; 
		
			// get the string up to and including the last real character
			resultString.Append (s.Substring (0, terminatorIndex + 1));
			// append a full stop 
			resultString.Append ('.');

		} else {
			// we don't want a terminator, but tbh we're allowed "?" and "!"
			if (s [terminatorIndex] != '.')
				return s; 

			// we're also allowed "...", "..!" and "..?"
			if (terminatorIndex >= 3 && s[terminatorIndex-1] == '.' &&  s[terminatorIndex-2] == '.' )
				return s;

			// get the string up to and excluding the last real character
			resultString.Append (s.Substring (0, terminatorIndex)); 
		}

		if (terminatorIndex < s.Length - 1) {
			// complete the string with the non-terminating cruft from the end
			resultString.Append (s.Substring (terminatorIndex + 1)); 
		}
		return resultString.ToString();


	}




    

	readonly static string[] Tens = { 
		"zero", "ten", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety"
	};
	readonly static string[] Units = { 
		"zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", 
		"ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen"
	};

	/// <summary>
	///  Generate a number in words
	/// </summary>
	/// <returns>The number as words.</returns>
	/// <param name="number">Number to convert</param>
	public static string NumberAsWords(int number, bool withLeadingConjunction = false) {
		var sb = new StringBuilder();

		if (number > 0) {
			if (withLeadingConjunction) {
				
				// we've just printed "two thousand..." 
				if (number < 100) {
					sb.Append (" and ");
				} else {
					sb.Append (", ");
				}
			}

			if (number >= 1000000) {
				
				sb.Append (NumberAsWords (Mathf.FloorToInt (number / 1000000)));
				sb.Append (" million");
				if (number % 1000 > 0) {
					sb.Append (NumberAsWords (number % 1000000, true));
				}
			} else if (number >= 1000) {
				sb.Append (NumberAsWords (Mathf.FloorToInt (number / 1000)));
				sb.Append (" thousand");
				if (number % 1000 > 0) {
					sb.Append (NumberAsWords (number % 1000, true));
				}
			} else if (number >= 100) {
				sb.Append (NumberAsWords (Mathf.FloorToInt (number / 100)));
				sb.Append (" hundred");
				if (number % 100 > 0) {
					sb.Append (NumberAsWords (number % 100, true));
				}
			} else {
				if (number >= 20) {
					sb.Append (Tens [Mathf.FloorToInt (number / 10)]);  // thirty
					if (number % 10 > 0) {
						sb.Append ("-"); 		// thirty-
					}
				}
				if (number >= 10 && number < 20) {
					sb.Append (Units [number]); // thirteen, fourteen, fifteen...
				} else if (number % 10 > 0) {
					sb.Append (Units [number % 10]);
				}
			}
		} else {
			if (!withLeadingConjunction) {
				sb.Append ("zero");
			}
		}
		return sb.ToString();
	}
    


	
    // Useful for creating line breaks in text so each line has a similar num characters
    public static string StringWithInsertedNewline(string str, int maxSingleLineCharacters) {
        if(str == null) return null;
        int length = str.Length;
        if(length <= maxSingleLineCharacters) return str;
        
        int mid = length/2;
        // Don't iterate all the characters, since it's not worth splitting at the very edges
        for(int i=0; i<length-2; ++i) {
            int offset = i/2;
            if( (i%2) == 1 ) {
                offset = -offset - 1;
            }
            int characterIndex = mid + offset;
            if( str[characterIndex] == ' ' ) {
                return str.Substring(0, characterIndex) + "\n" + str.Substring(characterIndex+1);
            }
        }
        return str;
    }
}