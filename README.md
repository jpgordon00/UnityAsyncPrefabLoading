# UnityAsyncPrefabLoading
Complete one-file async prefab loading and caching solution for Unity

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
