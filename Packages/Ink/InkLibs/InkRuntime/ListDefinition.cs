﻿#pragma warning disable IDE1006

using System.Collections.Generic;

namespace Ink.Runtime
{
    public class ListDefinition
    {
        public string name { get { return _name; } }

        public Dictionary<InkListItem, int> items {
            get {
                if (_items == null) {
                    _items = new Dictionary<InkListItem, int> ();
                    foreach (var itemNameAndValue in _itemNameToValues) {
                        var item = new InkListItem (name, itemNameAndValue.Key);
                        _items [item] = itemNameAndValue.Value;
                    }
                }
                return _items;
            }
        }
        Dictionary<InkListItem, int> _items;

        public int ValueForItem (InkListItem item)
        {
            int intVal;
            if (_itemNameToValues.TryGetValue (item.itemName, out intVal))
                return intVal;
            else
                return 0;
        }

        public bool ContainsItem (InkListItem item)
        {
            if (item.originName != name) return false;

            return _itemNameToValues.ContainsKey (item.itemName);
        }

        public bool ContainsItemWithName (string itemName)
        {
            return _itemNameToValues.ContainsKey (itemName);
        }

        public bool TryGetItemWithValue (int val, out InkListItem item)
        {
            foreach (var namedItem in _itemNameToValues) {
                if (namedItem.Value == val) {
                    item = new InkListItem (name, namedItem.Key);
                    return true;
                }
            }

            item = InkListItem.Null;
            return false;
        }

        public bool TryGetValueForItem (InkListItem item, out int intVal)
        {
            return _itemNameToValues.TryGetValue (item.itemName, out intVal);
        }

        public ListDefinition (string name, Dictionary<string, int> items)
        {
            _name = name;
            _itemNameToValues = items;
        }

        string _name;

        // The main representation should be simple item names rather than a RawListItem,
        // since we mainly want to access items based on their simple name, since that's
        // how they'll be most commonly requested from ink.
        Dictionary<string, int> _itemNameToValues;
    }
}
