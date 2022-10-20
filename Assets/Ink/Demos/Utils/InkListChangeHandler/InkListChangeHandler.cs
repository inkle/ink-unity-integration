using Ink.Runtime;
using System.Linq;
using System.Collections.Generic;

// This class provides callbacks when an InkList variable changes.
// An example use case might be updating views for an item inventory as items are added or removed.

/*
public InkListChangeHandler inventoryChangeHandler = new InkListChangeHandler("Inventory");

void OnEnable () {
    StoryManager.OnCreateStory += SubscribeToStory;
}
void OnDisable () {
    StoryManager.OnCreateStory -= SubscribeToStory;
}

void SubscribeToStory (Story story) {
    inventoryChangeHandler.SetStory(story, true);
}
*/

[System.Serializable]
public class InkListChangeHandler {
    Story story;
    
    [UnityEngine.SerializeField]
    string _variableName;
    public string variableName => _variableName;
    [UnityEngine.SerializeField]
    bool observing;

    InkList _inkList;
    public InkList inkList => _inkList;

    List<InkListItem> prevListItems = new List<InkListItem>();
    [UnityEngine.SerializeField]
    List<InkListItem> _currentListItems = new List<InkListItem>();
    public IReadOnlyList<InkListItem> currentListItems => _currentListItems;
    List<InkListItem> itemsAdded = new List<InkListItem>();
    List<InkListItem> itemsRemoved = new List<InkListItem>();

    public delegate void OnChangeDelegate(IReadOnlyList<InkListItem> currentListItems, IReadOnlyList<InkListItem> itemsAdded, IReadOnlyList<InkListItem> itemsRemoved);
    public OnChangeDelegate OnChange;
    
    public InkListChangeHandler (string variableName) {
        this._variableName = variableName;
    }
    
    // Sets the story that we want to track this variable for. Set silently true if you do not wish to get events from the values changed. 
    public void SetStory(Story newStory, bool silently) {
        RemoveVariableObserver();
        Clear();
        story = newStory;
        AddVariableObserver();
        RefreshValue(silently);
    }
    
    // Observes the variable for a given story instance.
    void AddVariableObserver () {
        if(observing) {
            UnityEngine.Debug.LogWarning("Tried observing story for variable with name "+_variableName+" but we're already observing. Please call RemoveVariableObserver (passing null if the story no longer exists is fine) first!");
            return;
        }
        story.ObserveVariable(variableName, OnInkVarChanged);
        observing = true;
    }
    
    // Un-observes the variable for the story instance that was originally passed to AddVariableObserver.
    // If the original story instance no longer exists, you can just pass null into here to reset the state of this class.
    void RemoveVariableObserver () {
        if(!observing) return;
        if(story != null) story.RemoveVariableObserver(OnInkVarChanged, _variableName);
        observing = false;
    }

    void Clear() {
        prevListItems.Clear();
        _currentListItems.Clear();
        itemsAdded.Clear();
        itemsRemoved.Clear();
    }
    
    // Manually refresh this handler's list values.
    void RefreshValue (bool silently) {
        OnInkVarChanged(variableName, story.variablesState[variableName], silently);
    }
    
    void OnInkVarChanged (string variableName, object newValue) {
        OnInkVarChanged(variableName, newValue, false);
    }
    void OnInkVarChanged (string variableName, object newValue, bool silently) {
        _inkList = (InkList)newValue;
        
        prevListItems.Clear();
        prevListItems.AddRange(_currentListItems);

        _currentListItems.Clear();
		foreach(var listItem in inkList)
			_currentListItems.Add(listItem.Key);
        
        if(GetChanges(prevListItems, _currentListItems, ref itemsRemoved, ref itemsAdded)) {
            if (!silently && OnChange != null) 
                OnChange(_currentListItems, itemsAdded, itemsRemoved);
        }
    }
    
    
    
    static bool GetChanges<T> (IEnumerable<T> oldList, IEnumerable<T> newList, ref List<T> itemsRemoved, ref List<T> itemsAdded) {
		if(itemsRemoved == null) itemsRemoved = new List<T>();
		if(itemsAdded == null) itemsAdded = new List<T>();
		
		GetRemovedNonAlloc(oldList, newList, itemsRemoved);
		GetAddedNonAlloc(oldList, newList, itemsAdded);

        return itemsRemoved.Count > 0 || itemsAdded.Count > 0;
	}

	static void GetRemovedNonAlloc<T> (IEnumerable<T> oldList, IEnumerable<T> newList, List<T> removedListToFill) {
        removedListToFill.Clear();
        if(oldList == null) return;
        foreach(var oldItem in oldList) {
            if(newList == null || !newList.Contains(oldItem)) {
                removedListToFill.Add(oldItem);
            }
        }
	}
	
	static void GetAddedNonAlloc<T> (IEnumerable<T> oldList, IEnumerable<T> newList, List<T> addedListToFill) {
        addedListToFill.Clear();
		if(newList == null) return;
        foreach(var newItem in newList) {
            if(oldList == null || !oldList.Contains(newItem)) {
                addedListToFill.Add(newItem);
            }
        }
	}
}