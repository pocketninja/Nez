using System.Collections;


namespace Nez.Tweens
{
	/// <summary>
	/// more specific tween playback controls here.
	/// </summary>
	public interface ITweenControl : ITweenable
	{
		/// <summary>
		/// handy property that you can use in any callbacks (such as a completion handler) to avoid allocations when using
		/// anonymous Actions
		/// </summary>
		/// <value>The context.</value>
		object Context { get; }

		/// <summary>
		/// warps the tween to elapsedTime clamping it between 0 and duration. this will immediately update the tweened
		/// object whether it is paused, completed or running.
		/// </summary>
		/// <param name="elapsedTime">Elapsed time.</param>
		void JumpToElapsedTime(float elapsedTime);

		/// <summary>
		/// when called from StartCoroutine it will yield until the tween is complete
		/// </summary>
		/// <returns>The for completion.</returns>
		IEnumerator WaitForCompletion();

		/// <summary>
		/// gets the target of the tween or null for TweenTargets that arent necessarily all about a single object.
		/// its only real use is for TweenManager to find a list of tweens by target.
		/// </summary>
		/// <returns>The target object.</returns>
		object GetTargetObject();

		/// <summary>
		/// Get the elapsed time of the tween.
		/// </summary>
		/// <returns>the elapsed time.</returns>
		float ElapsedTime();

		/// <summary>
		/// Get the elapsed time of the tween as a value between 0=>1 (ie, percentage)
		/// </summary>
		/// <returns>the elapsed time between 0=>1</returns>
		float ElapsedTime01();

		/// <summary>
		/// Get the duration of the tween.
		/// </summary>
		/// <returns>the duration</returns>
		float Duration();

		/// <summary>
		/// Get the time remaining.
		/// </summary>
		/// <returns>the time remaining</returns>
		float TimeRemaining();
		
	}
}