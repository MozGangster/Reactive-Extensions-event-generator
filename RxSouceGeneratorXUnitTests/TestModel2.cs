namespace RxSouceGeneratorXUnitTests;

public class TestModel2
{
    public delegate void AccountHandler(string message);
    /// <summary>
    /// event AccountHandler AccountHandlerEvent;
    /// </summary>
    public event AccountHandler AccountHandlerEvent;

    /// <summary>
    /// event Action<int, string, bool, SomeEventArgs> ActionEvent1;
    /// </summary>
    public event Action<int, string, bool, SomeEventArgs> ActionEvent1;

    /// <summary>
    /// Action ActionEvent2;
    /// </summary>
    public event Action ActionEvent2;

    /// <summary>
    /// EventHandler EventHandlerEvent1
    /// </summary>
    public event EventHandler EventHandlerEvent1;

    /// <summary>
    /// event EventHandler<SomeEventArgs> EventHandlerEvent2
    /// </summary>
    public event EventHandler<SomeEventArgs> EventHandlerEvent2;

    /// <summary>
    /// event Action<int, string, bool, SomeEventArgs> ActionStub;
    /// </summary>
    public event Action<int, string, bool, SomeEventArgs> ActionStub;
    public void FireAccountHandlerEvent()
    {
        AccountHandlerEvent?.Invoke("Fire");
    }

    public void FireActionEvent1()
    {
        ActionEvent1?.Invoke(10, "Fire", true, new SomeEventArgs());
    }

    public void FireActionEvent2()
    {
        ActionEvent2?.Invoke();
    }

    public void FireEventHandlerEvent1()
    {
        EventHandlerEvent1?.Invoke(this, new EventArgs());
    }

    public void FireEventHandlerEvent2()
    {
        EventHandlerEvent2?.Invoke(this, new SomeEventArgs());
    }
}