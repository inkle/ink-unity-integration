using UnityEngine;

namespace AssetStoreTools.Validator
{
    internal class AutomatedTest : ValidationTest
    {
        public AutomatedTest(ValidationTestScriptableObject source) : base(source) { }

        public override void Run()
        {
            var actionsObject = TestActions.Instance;
            var method = actionsObject.GetType().GetMethod(TestMethodName);
            if (method != null)
            {
                Result = (TestResult)method.Invoke(actionsObject, null);
                OnTestCompleted();
            }
            else
            {
                Debug.LogError("Cannot invoke method \"" + TestMethodName + "\". No such method found");
            }
        }
    }
}