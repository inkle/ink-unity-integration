namespace Ink.Parsed {
    public class Identifier {
        public string name;
        public Runtime.DebugMetadata debugMetadata;

        public override string ToString()
        {
            return name;
        }

#pragma warning disable IDE0090 // Use 'new(...)'
        public static Identifier Done = new Identifier { name = "DONE", debugMetadata = null };
#pragma warning restore IDE0090 // Use 'new(...)'
    }
}
