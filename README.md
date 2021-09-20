# UnityAsyncPrefabLoading
A complete one-file async prefab loading and caching solution for Unity

## What does it do?
- Load any amount of Prefabs at a time from Resources in a non blocking call
- Optional resource caching that avoids double resource loading
- Simple callbacks for individual prefabs loaded and for groups of prefab loaded
- Optional and automatic async GO instantition 
- Calculates simple progress calculation and timer

## Why?
- Avoid Prefab loading lag by having both loading and instantiation in an asyncronous call
- Execute code when groups of prefabs have finished asyncronously with optional C# actions
- Cache prefabs by keeping inactive parent objects in the scene and by avoiding Resources.Load

## Usage
- Load prefabs asyncronously with a single callback that is invoked when all the prefabs are loaded.
```javascript
// strings in the first parameter must match files in 'Resources'
GroupLoader.Instance.LoadResources(new List<string>{"prefab1", "level/prefab", "prefab2"}, 
  () => {
    Debug.Log("All prefabs loaded!");
  });
```
- Load prefabs asyncronously with a callback for each prefab and a single callback that is invoked when all the prefabs are loaded.
```javascript
// strings in the first parameter must match files in 'Resources'
GroupLoader.Instance.LoadResources(new List<Resource>{
  new Resource{
    Name = "level/prefab",
    FinishCallback = () => {
      Debug.Log("Resource loaded: " + this.Name);
    }
  },
  new Resource{
    Name = "prefab1",
    FinishCallback = () => {
      Debug.Log("Resource loaded: " + this.Name);
    }
  }
}, 
  () => {
    Debug.Log("All prefabs loaded!");
  });
```
- Load prefabs asyncronously, caching some prefabs, with a callback for each prefab and a single callback that is invoked when all the prefabs are loaded.
```javascript
// strings in the first parameter must match files in 'Resources'
GroupLoader.Instance.LoadResources(new List<Resource>{
  new Resource{
    Name = "level/prefab",
    CacheResult = true,
    FinishCallback = () => {
      Debug.Log("Resource loaded: " + this.Name);
    }
  },
  new Resource{
    Name = "prefab1",
    FinishCallback = () => {
      Debug.Log("Resource loaded: " + this.Name);
    }
  }
}, 
  () => {
    Debug.Log("All prefabs loaded!");
  });
```
