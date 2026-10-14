namespace DVG.SkyPirates.Shared.Core
{
	public interface IView { }

	public interface IView<VM> : IView
		where VM : IViewModel
	{
		VM ViewModel { get; set; }
	}
}
