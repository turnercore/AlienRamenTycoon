using System;
using System.Reflection;

public abstract class Singleton<T> where T : Singleton<T> {
    protected Singleton() { }
    public static T Instance { get; } = Create();

    // using reflection to create an instance of the class without using new()
    private static T Create() {
        Type t = typeof(T);
        BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        ConstructorInfo constructor = t.GetConstructor(flags, null, Type.EmptyTypes, null);
        object instance = constructor.Invoke(null);
        return instance as T;
    }
}

public class FirstSingleton : Singleton<FirstSingleton> {
    private FirstSingleton() { } // private constructor to make sure this class it cannot be instantiated by using new()
    public void DoSomething() { }
}

public class AnotherSingleton : Singleton<AnotherSingleton> {
    private AnotherSingleton() { }

    public void DoSomething() {
        FirstSingleton.Instance.DoSomething();
    }
}
