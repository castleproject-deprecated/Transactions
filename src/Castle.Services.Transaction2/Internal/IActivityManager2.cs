namespace Castle.Services.Transaction2.Internal
{
	public interface IActivityManager2
	{
		bool TryGetCurrentActivity(out Activity2 activity);

		Activity2 EnsureActivityExists();

		void NotifyPop(Activity2 activity2);
	}
}