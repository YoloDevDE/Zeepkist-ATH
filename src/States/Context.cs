namespace AuthorTimeHunting.States;

public abstract class Context
{
    public abstract PluginState State { get; set; }

    public virtual void SwitchState(PluginState pluginState)
    {
        State.Exit();
        State = pluginState;
        State.Enter();
    }
}