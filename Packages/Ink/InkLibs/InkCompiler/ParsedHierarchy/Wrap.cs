
namespace Ink.Parsed
{
    public class Wrap<T> : Parsed.Object where T : Runtime.Object
    {
        public Wrap (T objToWrap)
        {
            _objToWrap = objToWrap;
        }

        public override Runtime.Object GenerateRuntimeObject ()
        {
            return _objToWrap;
        }

#pragma warning disable IDE0044 // Add readonly modifier
        T _objToWrap;
#pragma warning restore IDE0044 // Add readonly modifier
    }

    // Shorthand for writing Parsed.Wrap<Runtime.Glue> and Parsed.Wrap<Runtime.Tag>
    public class Glue : Wrap<Runtime.Glue> {
        public Glue (Runtime.Glue glue) : base(glue) {}
    }
    public class Tag : Wrap<Runtime.Tag> {
        public Tag (Runtime.Tag tag) : base (tag) { }
    }
}

