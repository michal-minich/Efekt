namespace Elab.Actions
{
    public interface IElabAction
    {
        string Name { get; }
        bool CanIvoke();
        void Invoke();
    }
}