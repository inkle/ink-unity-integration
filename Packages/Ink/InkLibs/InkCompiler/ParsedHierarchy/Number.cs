
namespace Ink.Parsed
{
	public class Number : Parsed.Expression
	{
		public object value;
		
		public Number(object value)
		{
            if (value is int || value is float || value is bool) {
                this.value = value;
            } else {
                throw new System.Exception ("Unexpected object type in Number");
            }
		}

        public override void GenerateIntoContainer (Runtime.Container container)
		{
#pragma warning disable IDE0038 // Use pattern matching
            if (value is int) {
#pragma warning restore IDE0038 // Use pattern matching
                container.AddContent (new Runtime.IntValue ((int)value));
#pragma warning disable IDE0038 // Use pattern matching
            } else if (value is float) {
#pragma warning restore IDE0038 // Use pattern matching
                container.AddContent (new Runtime.FloatValue ((float)value));
#pragma warning disable IDE0038 // Use pattern matching
            } else if(value is bool) {
#pragma warning restore IDE0038 // Use pattern matching
                container.AddContent (new Runtime.BoolValue ((bool)value));
            }
		}

        public override string ToString ()
        {
#pragma warning disable IDE0038 // Use pattern matching
            if (value is float) {
#pragma warning restore IDE0038 // Use pattern matching
                return ((float)value).ToString(System.Globalization.CultureInfo.InvariantCulture);
            } else {
                return value.ToString();
            }
        }

        // Equals override necessary in order to check for CONST multiple definition equality
        public override bool Equals (object obj)
        {
            var otherNum = obj as Number;
            if (otherNum == null) return false;

            return this.value.Equals (otherNum.value);
        }

        public override int GetHashCode ()
        {
            return this.value.GetHashCode ();
        }
         
	}
}

