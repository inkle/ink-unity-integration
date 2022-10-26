using System.Linq;
using UnityEngine;

namespace AssetStoreTools.Validator
{
    [System.Serializable]
    internal class ValidatorCategory
    {
        public bool AppliesToAll => Filter.Length == 0 && !FilterIsInclusive;
        public string[] Filter = { "Tools", "Art" };
        [Tooltip("Tip: When disabled and filter is empty, this category will apply to all")]
        public bool FilterIsInclusive = true;
        public bool AppliesToSubCategories = true;

        public bool AppliesToCategory(string category)
        {
            if (AppliesToAll)
                return true;
            
            if(AppliesToSubCategories)
                category = category.Split('/')[0];

            var applies = FilterIsInclusive;
            if(Filter.All(x => x != category))
                applies = !applies;
            return applies;
        }
    }
}