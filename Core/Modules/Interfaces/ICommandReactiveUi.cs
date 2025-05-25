namespace Modules.Interfaces;
public interface ICommandReactiveUi
{
  ReactiveCommand<string, Unit> CommandNavigate { get; }
  void Navigate(string command);
}
