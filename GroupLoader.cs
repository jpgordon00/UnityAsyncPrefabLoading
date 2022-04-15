using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System.Linq;
using UnityEditor;
using Newtonsoft.Json;

/*
    Used to optionally specify parameters for a specific resource
    when calling 'LoadResources'
    Automatically loads cached resources from memory
*/
public class Resource {
    public string Name; // the resource full path
    public bool CacheResult = false; // true to cache the resulting object
    public Action<object> FinishCallback; // callback invoked when the resulting object is loaded from disk 
    public bool InstantiateResult = false;
    public Transform Transform;
}

/*
    Used internally to store each resource request.
*/
internal class LoadingResource {
    public string Name; // the resource full path
    public object Result; // the stored 'object' after loading.
    public bool CacheResult; // true to store into a cache afdter loading
    public bool InstantiateResult; // true to instantiate  the object and return the GameObject in the scene 
    public Action<object> FinishCallback; // callback invoked with the resulting loaded object from disk
    public Transform Transform = null;
}

/*
    Loads any amount of resources at a time asyncronously with callbacks.
    Caches resulting objects and deletes upon cleanup (or upon new download invokation)
    Timers and progress calculation.
    Access cache through 'GetResource'
    TODO: weighted progress for more accurate progress calculation
*/
public class GroupLoader
{

    /*
        Singleton design pattern
    */
    private static GroupLoader _instance;

    public static GroupLoader Instance {
        get {
            if (_instance == null) _instance = new GroupLoader();
            return _instance;
        }
    }

    // static events for file loading
    public event Action StaticFinishCallback;

    /*
        Local list of pending resources to load.
        This list is emptied after a failed or succesful load call.
    */
    private List<LoadingResource> _resources = new List<LoadingResource>();

    public Action FinishCallback; // invoked when the resource handler finishes loading a group of resources 

    /*
        Stopwatch for an occuring past download.
    */
    public Stopwatch StopWatch = new Stopwatch();

    /*
        Percentage complete represented by a float 0f -> 1f
    */
    private float _progress = 0f;

    /*
        Total amount of items to load, used to calculate progress.
    */
    public float _numResources;

    public float Progress {
        get {
            return _progress;
        }
    }

    /*
        Elapsed time in seconds or 0
    */
    public double Elapsed {
        get {
            return StopWatch.Elapsed.TotalSeconds;
        }
    }

    /*
        Loads a list of resouces with caching disabled.
    */
    public async void LoadResources(List<string> resources, Action finishCallback = null) {
        if (Loading) return; // case currently loading
        FinishCallback = finishCallback;
        _loading = true;
        _numResources = (float) resources.Count;
        StopWatch.Start();
        foreach (var str in resources) LoadResource(str);
        while (!IsAllDone()) {
            await Task.Delay(100);
        }
        _loading = false;
        StopWatch.Stop();
        if (finishCallback != null) {
            finishCallback();
            StaticFinishCallback?.Invoke();
        }
    }

    /*
        Loads a list of resources where caching is specified for each file.
    */
    public async void LoadResources(List<Resource> resources, Action finishCallback = null) {
        if (Loading) return; // case currently loading
        FinishCallback = finishCallback;
        _loading = true; 
        _numResources = (float) resources.Count;
        StopWatch.Start();
        foreach (var resource in resources) LoadResource(resource.Name, resource.FinishCallback, resource.CacheResult, resource.InstantiateResult, resource.Transform);
        while (!IsAllDone()) {
            await Task.Delay(75);
        }
        StopWatch.Stop();
        _loading = false;
        if (finishCallback != null) {
            finishCallback();
            StaticFinishCallback?.Invoke();
        }
    }

    /*
        Loads a Resource asyncronously by name.
        Calls the callback with the cached resource if possible.
        If a cache already exists cleanup first.
    */
    private async void LoadResource(string name, Action<object> finishCallback = null, bool cacheResult = false, bool instantiateResult = false, Transform transform = null) {
        if (HasResource(name)) {
            // case: load from cache instead
            _progress += 1 / _numResources;
            var ir = _resources.Find(r => r.Name == name);
            // case: has resource but isn't finished loading async yet
            while (ir.Result == null) await Task.Delay(100); // wait for async loading
            if (finishCallback != null) finishCallback.Invoke(ir.Result);
            return;
        }
        _resources.Add(new LoadingResource {Name = name, FinishCallback = finishCallback, CacheResult = cacheResult, InstantiateResult = instantiateResult});
        var resourceRequest = Resources.LoadAsync(name);
        // update progress after everything is complete
        resourceRequest.completed += o =>
        {
            // case: instantiate object, callback with GO, still set result to loaded prefab
            if (_resources.Find(lr => lr.Name == name).InstantiateResult) {
                var go = transform == null ? UnityEngine.GameObject.Instantiate(resourceRequest.asset) : UnityEngine.GameObject.Instantiate(resourceRequest.asset, transform);
                if (finishCallback != null) finishCallback.Invoke(go);
            } else {
                if (finishCallback != null) finishCallback.Invoke(resourceRequest.asset); // invoke finish callback for this resource
            }
            _resources[_resources.IndexOf(_resources.Find(lr => lr.Name == name))].Result = resourceRequest.asset; // set this assets result obj
            if (!_resources.Find(lr => lr.Name == name).CacheResult)  _resources.Remove(_resources.Find(lr => lr.Name == name)); // delete non cached items
            _progress += 1 / _numResources;
        };
    }

    /*
        True only if this handler is currently loading resources;
    */
    private bool _loading = false;
    public bool Loading {
        get => _loading;
    }
    
    /*
     Gets a resource from the cache
    */
    public object GetResource(string name) {
        return _resources.Find(lr => lr.Name == name);
    }

    /*
        Returns true only if the cahce has a resource by name
    */
    public bool HasResource(string name) {
        return _resources.Find(lr => lr.Name == name) != null;
    }

    /*
        Returns true only if a given resource is finished loading.
        False if not resource is found.
    */
    public bool IsDone(string name) {
        if (_resources.Find(lr => lr.Name == name) == null) return false; // case resource does not exist
        return _resources.Find(lr => lr.Name == name).Result != null;
    }

    /*
        True only if all resources are done loading.
    */
    public bool IsAllDone() {
        // case: all resources are done loading
        bool complete = true;
        foreach (var resource in _resources) {
            if (resource.Result == null) complete = false;
        }
        return complete;
    }

    /*
        Destroys all cached objects.
        Resets timers.
    */
    public void Cleanup() {
        _resources.Clear();
        StopWatch = new Stopwatch();
        _loading = false;
        _progress = 0f;
        _numResources = 0;
    }

}
